using ClosedXML.Excel;
using Microsoft.Data.Sqlite;

namespace AracSatisSistemi.Services
{
    // Veritabanının günlük, haftalık (her Çarşamba akşamı) ve aylık (her ayın 1'i)
    // otomatik yedeğini alır. Uygulama günlerce yeniden başlamadan açık kalsa bile
    // (dotnet watch veya sunucuda systemd ile) periyodik kontrol sayesinde yedekler
    // alınmaya devam eder - yalnızca "uygulama açılışında bir kere" yedek almak,
    // uygulama hiç kapanmadan günler/haftalar geçerse yetersiz kalırdı.
    //
    // - Günlük yedekler Backups/Gunluk/ klasörüne, günde bir kez alınır; 30 günden
    //   eski olanlar otomatik silinir.
    // - Haftalık yedekler Backups/Haftalik/ klasörüne, her Çarşamba akşamı (saat
    //   18:00'dan sonra) bir kez alınır ve HİÇBİR ZAMAN silinmez (kalıcı arşiv).
    // - Aylık yedekler Backups/Aylik/ klasörüne, her ayın 1'inde bir kez alınır ve
    //   HİÇBİR ZAMAN silinmez (kalıcı arşiv).
    //
    // Her .db yedeğinin yanında, kullanıcının Excel ile kolayca açıp kontrol edebilmesi
    // için aynı veriyi içeren bir .xlsx kopyası da üretilir (her tablo ayrı bir sayfada).
    // Access (.accdb) formatında yedek üretilmiyor; bunun için Microsoft'un Windows'a özel
    // Access Database Engine'i gerekir, Linux sunucuda güvenilir şekilde çalıştırılamaz -
    // Excel aynı ihtiyacı (veriyi kolayca görüntüleyebilme) zaten karşılar.
    //
    // ÖNEMLİ: Veritabanı WAL (Write-Ahead Log) modunda çalıştığından, en güncel veri bir
    // süre yalnızca "AracSatis.db-wal" dosyasında durur, ana "AracSatis.db" dosyasına
    // hemen yazılmaz. Bu yüzden düz bir dosya kopyalama (File.Copy) EKSİK/BOZUK bir yedek
    // üretebilirdi. Bunun yerine SQLite'ın "VACUUM INTO" komutu kullanılır: canlı veritabanı
    // çalışırken bile WAL dahil TÜM veriyi tutarlı, tek parça bir dosyaya güvenle aktarır.
    public class YedeklemeServisi : BackgroundService
    {
        private const int GunlukSaklamaGunSayisi = 30;
        private const int HaftalikYedekSaati = 18; // "akşam" - saat 18:00'dan itibaren
        private static readonly TimeSpan KontrolAraligi = TimeSpan.FromHours(1);

        private readonly string _dbYolu;
        private readonly string _gunlukKlasor;
        private readonly string _haftalikKlasor;
        private readonly string _aylikKlasor;
        private readonly ILogger<YedeklemeServisi> _logger;

        public YedeklemeServisi(IWebHostEnvironment env, ILogger<YedeklemeServisi> logger)
        {
            _dbYolu = Path.Combine(env.ContentRootPath, "AracSatis.db");
            var kokKlasor = Path.Combine(env.ContentRootPath, "Backups");
            _gunlukKlasor = Path.Combine(kokKlasor, "Gunluk");
            _haftalikKlasor = Path.Combine(kokKlasor, "Haftalik");
            _aylikKlasor = Path.Combine(kokKlasor, "Aylik");
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    YedekAl();
                }
                catch (Exception ex)
                {
                    // Yedekleme başarısız olsa bile uygulama/servis çalışmaya devam etsin.
                    _logger.LogError(ex, "Otomatik veritabanı yedeklemesi başarısız oldu.");
                }

                try
                {
                    await Task.Delay(KontrolAraligi, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Uygulama kapatılırken normal.
                }
            }
        }

        private void YedekAl()
        {
            if (!File.Exists(_dbYolu)) return;

            Directory.CreateDirectory(_gunlukKlasor);
            Directory.CreateDirectory(_haftalikKlasor);
            Directory.CreateDirectory(_aylikKlasor);

            var bugun = DateTime.Now;

            // GÜNLÜK: bugüne ait bir yedek yoksa al.
            var gunlukYol = Path.Combine(_gunlukKlasor, $"AracSatis_{bugun:yyyyMMdd}.db");
            if (!File.Exists(gunlukYol))
            {
                VeritabaniniYedekle(gunlukYol);
                ExceleAktar(gunlukYol);
            }

            // 30 günden eski günlük yedekler (.db ve .xlsx) otomatik silinir.
            var sinirTarihi = bugun.Date.AddDays(-GunlukSaklamaGunSayisi);
            foreach (var dosya in Directory.GetFiles(_gunlukKlasor, "AracSatis_*.*"))
            {
                if (File.GetLastWriteTime(dosya) < sinirTarihi) File.Delete(dosya);
            }

            // HAFTALIK: bugün Çarşamba ve saat 18:00'dan sonraysa, bu Çarşamba için henüz
            // yedek alınmadıysa al. Bu klasördeki dosyalar hiçbir zaman silinmez (kalıcı arşiv).
            if (bugun.DayOfWeek == DayOfWeek.Wednesday && bugun.Hour >= HaftalikYedekSaati)
            {
                var haftalikYol = Path.Combine(_haftalikKlasor, $"AracSatis_Haftalik_{bugun:yyyyMMdd}.db");
                if (!File.Exists(haftalikYol))
                {
                    VeritabaniniYedekle(haftalikYol);
                    ExceleAktar(haftalikYol);
                }
            }

            // AYLIK: ayın 1'indeysek ve bu ay için henüz yedek alınmadıysa al. Bu klasördeki
            // dosyalar hiçbir zaman silinmez (kalıcı arşiv).
            if (bugun.Day == 1)
            {
                var aylikYol = Path.Combine(_aylikKlasor, $"AracSatis_Aylik_{bugun:yyyyMM}.db");
                if (!File.Exists(aylikYol))
                {
                    VeritabaniniYedekle(aylikYol);
                    ExceleAktar(aylikYol);
                }
            }
        }

        // Canlı veritabanı WAL modunda çalışırken bile tutarlı ve eksiksiz bir yedek almak
        // için SQLite'ın "VACUUM INTO" komutunu kullanır (bkz. yukarıdaki not). Düz dosya
        // kopyalamadan farklı olarak WAL'da bekleyen en güncel veriyi de içerir.
        private void VeritabaniniYedekle(string hedefYol)
        {
            if (File.Exists(hedefYol)) File.Delete(hedefYol); // VACUUM INTO hedef dosya zaten varsa hata verir.
            using var baglanti = new SqliteConnection($"Data Source={_dbYolu}");
            baglanti.Open();
            using var komut = baglanti.CreateCommand();
            komut.CommandText = "VACUUM INTO $hedef";
            komut.Parameters.AddWithValue("$hedef", hedefYol);
            komut.ExecuteNonQuery();
        }

        // Verilen .db yedeğindeki tüm tabloları, yanına aynı adla .xlsx olarak (her tablo
        // ayrı bir sayfada) aktarır. Canlı veritabanına değil, az önce alınan DURAĞAN yedek
        // kopyasına bağlanır; böylece okurken kilitlenme/çakışma riski olmaz.
        private void ExceleAktar(string dbYedekYolu)
        {
            try
            {
                var excelYolu = Path.ChangeExtension(dbYedekYolu, ".xlsx");
                using var baglanti = new SqliteConnection($"Data Source={dbYedekYolu};Mode=ReadOnly");
                baglanti.Open();

                var tabloAdlari = new List<string>();
                using (var tabloKomutu = baglanti.CreateCommand())
                {
                    tabloKomutu.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name != '__EFMigrationsHistory' ORDER BY name";
                    using var okuyucu = tabloKomutu.ExecuteReader();
                    while (okuyucu.Read()) tabloAdlari.Add(okuyucu.GetString(0));
                }

                using var kitap = new XLWorkbook();
                foreach (var tablo in tabloAdlari)
                {
                    var sayfa = kitap.Worksheets.Add(tablo.Length > 31 ? tablo[..31] : tablo);
                    using var komut = baglanti.CreateCommand();
                    komut.CommandText = $"SELECT * FROM \"{tablo}\"";
                    using var okuyucu = komut.ExecuteReader();

                    for (int s = 0; s < okuyucu.FieldCount; s++)
                        sayfa.Cell(1, s + 1).Value = okuyucu.GetName(s);

                    int satir = 2;
                    while (okuyucu.Read())
                    {
                        for (int s = 0; s < okuyucu.FieldCount; s++)
                        {
                            var deger = okuyucu.IsDBNull(s) ? null : okuyucu.GetValue(s);
                            sayfa.Cell(satir, s + 1).Value = deger switch
                            {
                                null => XLCellValue.FromObject(string.Empty),
                                long l => l,
                                double d => d,
                                decimal m => m,
                                DateTime dt => dt,
                                bool b => b,
                                _ => deger.ToString() ?? string.Empty
                            };
                        }
                        satir++;
                    }
                    sayfa.Row(1).Style.Font.Bold = true;
                    if (okuyucu.FieldCount > 0) sayfa.Columns(1, okuyucu.FieldCount).AdjustToContents(1, 200);
                }

                kitap.SaveAs(excelYolu);
            }
            catch (Exception ex)
            {
                // Excel aktarımı başarısız olsa bile asıl .db yedeği zaten alınmış durumda;
                // bu yüzden hata yutulup sadece loglanır.
                _logger.LogError(ex, "Yedeğin Excel kopyası oluşturulamadı: {DbYolu}", dbYedekYolu);
            }
        }
    }
}
