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
                File.Copy(_dbYolu, gunlukYol, overwrite: true);
            }

            // 30 günden eski günlük yedekler otomatik silinir.
            var sinirTarihi = bugun.Date.AddDays(-GunlukSaklamaGunSayisi);
            foreach (var dosya in Directory.GetFiles(_gunlukKlasor, "AracSatis_*.db"))
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
                    File.Copy(_dbYolu, haftalikYol, overwrite: true);
                }
            }

            // AYLIK: ayın 1'indeysek ve bu ay için henüz yedek alınmadıysa al. Bu klasördeki
            // dosyalar hiçbir zaman silinmez (kalıcı arşiv).
            if (bugun.Day == 1)
            {
                var aylikYol = Path.Combine(_aylikKlasor, $"AracSatis_Aylik_{bugun:yyyyMM}.db");
                if (!File.Exists(aylikYol))
                {
                    File.Copy(_dbYolu, aylikYol, overwrite: true);
                }
            }
        }
    }
}
