using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;
using ClosedXML.Excel;

namespace AracSatisSistemi.Controllers
{
    // TUTANAK VE RAPORLAR ekranı: dosya no, plaka, satış türü/tarihi/sonucu, vergi
    // dairesi ve durum kriterlerine göre çok yönlü filtrelenebilen tek bir liste;
    // listelenen dosyalar için toplu İhale Tutanağı yazdırma/PDF/Excel çıktısı.
    public class TutanakRaporController : Controller
    {
        private readonly AppDbContext _db;
        public TutanakRaporController(AppDbContext db) => _db = db;

        public class SatirSonuc
        {
            public int DosyaId { get; set; }
            public string DosyaNo { get; set; } = string.Empty;
            public string Plaka { get; set; } = string.Empty;
            public string AracCinsi { get; set; } = string.Empty;
            public string? AracTipi { get; set; }
            public string AracMarkasi { get; set; } = string.Empty;
            public int ModelYili { get; set; }
            public decimal MuhammenBedel { get; set; }
            public SatisTuru? SatisTuru { get; set; }
            public DateTime? SatisTarihi { get; set; }
            public SatisSonucu? SatisSonucu { get; set; }

            public string AdiSoyadiUnvani { get; set; } = string.Empty;
            public string? VergiKimlikNumarasi { get; set; }
            public string? TCKimlikNumarasi { get; set; }
            public string VergiDairesiAdi { get; set; } = string.Empty;
            public string AlacakliVergiDairesi { get; set; } = string.Empty;
            public string? KomisyonBaskani { get; set; }
            public string? KomisyonUye1 { get; set; }
            public string? KomisyonUye2 { get; set; }
            public string? KomisyonUye3 { get; set; }

            // İlgili Komisyon İşlemleri kaydından (varsa) bulunan ihale saati.
            public TimeSpan? SatisSaati { get; set; }

            public string SatisTuruGorunen => SatisTuru switch
            {
                AracSatisSistemi.Models.SatisTuru.BirinciSatis => "1. Satış",
                AracSatisSistemi.Models.SatisTuru.IkinciSatis => "2. Satış",
                AracSatisSistemi.Models.SatisTuru.Pazarlik => "Pazarlık",
                AracSatisSistemi.Models.SatisTuru.Madde6183_86 => "6183/86 Mad.",
                _ => "-"
            };
            // Bu dosya için henüz hiç satış sonucu girilmemişse (hâlâ 1. Satış'ı bekliyorsa),
            // Satış Türü/Tarihi/SONUÇ sütunları "-" yerine planlanan 1. Satış bilgisiyle
            // dolu gösterilsin diye; bkz. FiltreliListeGetir.
            public bool HenuzSonucGirilmedi { get; set; }

            public string SonucGorunen => SatisSonucu switch
            {
                AracSatisSistemi.Models.SatisSonucu.Satildi => "Satıldı",
                AracSatisSistemi.Models.SatisSonucu.AliciCikmadi => "Alıcı Çıkmadı",
                AracSatisSistemi.Models.SatisSonucu.AliciIslemYapmadi => "Alıcı İşlem Yapmadı",
                _ => HenuzSonucGirilmedi ? "Bekliyor" : "-"
            };
        }

        // Dosya.Asama'nın (henüz sonuçlanmamış) işaret ettiği satış türü - bu türün "Bekliyor"
        // satırını listeye eklemek için kullanılır; eşleşmeyen aşamalar (Alıcıya Terk Bekliyor,
        // Süreç Tamamlandı) için bekleyen bir satış türü yoktur.
        private static readonly Dictionary<Dosya.SurecAsamasi, SatisTuru> AsamaBekleyenTur = new()
        {
            [Dosya.SurecAsamasi.BirinciSatisBekliyor] = SatisTuru.BirinciSatis,
            [Dosya.SurecAsamasi.IkinciSatisBekliyor] = SatisTuru.IkinciSatis,
            [Dosya.SurecAsamasi.PazarlikBekliyor] = SatisTuru.Pazarlik,
            [Dosya.SurecAsamasi.Madde6183_86Bekliyor] = SatisTuru.Madde6183_86
        };

        private async Task<List<SatirSonuc>> FiltreliListeGetir(
            string? dosyaNo, SatisTuru? satisTuru, string? plaka, DateTime? satisTarihi,
            string? vergiDairesi, SatisSonucu? satisSonucu, string durum)
        {
            // Dallanma mantığı (Dosya.Asama) SQL'e çevrilemediğinden, eşleşen dosyalar belleğe
            // alınıp orada süzülür (bkz. IhaleGorunumleriGetir'deki aynı yaklaşım) - bu sayede
            // henüz hiç sonuç girilmemiş (ör. 1. Satış'ı bekleyen) dosyalar da "Bekliyor" satırı
            // olarak gösterilebilir, sadece geçmiş sonuçlar değil.
            var sorgu = _db.Dosyalar.Include(d => d.Satislar).AsQueryable();
            if (!string.IsNullOrWhiteSpace(dosyaNo)) sorgu = sorgu.Where(d => d.DosyaNo == dosyaNo);
            if (!string.IsNullOrWhiteSpace(plaka)) sorgu = sorgu.Where(d => d.Plaka == plaka);
            if (!string.IsNullOrWhiteSpace(vergiDairesi)) sorgu = sorgu.Where(d => d.VergiDairesiAdi == vergiDairesi);
            if (durum == "aktif")
                sorgu = sorgu.Where(d => d.Durum == DosyaDurumu.Kayitli || d.Durum == DosyaDurumu.SatisaCikti);
            else if (durum == "pasif")
                sorgu = sorgu.Where(d => d.Durum == DosyaDurumu.Satildi || d.Durum == DosyaDurumu.IptalEdildi
                    || d.Durum == DosyaDurumu.IadeEdildi || d.Durum == DosyaDurumu.Kapandi
                    || d.Durum == DosyaDurumu.AliciyaTerkEdildi);

            var dosyalar = await sorgu.ToListAsync();

            SatirSonuc DosyaAlanlariniKopyala(Dosya d) => new()
            {
                DosyaId = d.Id,
                DosyaNo = d.DosyaNo,
                Plaka = d.Plaka,
                AracCinsi = d.AracCinsi,
                AracTipi = d.AracTipi,
                AracMarkasi = d.AracMarkasi,
                ModelYili = d.ModelYili,
                MuhammenBedel = d.MuhammenBedel,
                AdiSoyadiUnvani = d.AdiSoyadiUnvani,
                VergiKimlikNumarasi = d.VergiKimlikNumarasi,
                TCKimlikNumarasi = d.TCKimlikNumarasi,
                VergiDairesiAdi = d.VergiDairesiAdi,
                AlacakliVergiDairesi = d.AlacakliVergiDairesi,
                KomisyonBaskani = d.KomisyonBaskani,
                KomisyonUye1 = d.KomisyonUye1,
                KomisyonUye2 = d.KomisyonUye2,
                KomisyonUye3 = d.KomisyonUye3
            };

            var liste = new List<SatirSonuc>();
            foreach (var d in dosyalar)
            {
                var eklendi = false;
                foreach (var s in d.Satislar)
                {
                    if (satisTuru.HasValue && s.SatisTuru != satisTuru.Value) continue;
                    if (satisTarihi.HasValue && s.SatisTarihi.Date != satisTarihi.Value.Date) continue;
                    if (satisSonucu.HasValue && s.SatisSonucu != satisSonucu.Value) continue;

                    var satir = DosyaAlanlariniKopyala(d);
                    satir.SatisTuru = s.SatisTuru;
                    satir.SatisTarihi = s.SatisTarihi;
                    satir.SatisSonucu = s.SatisSonucu;
                    liste.Add(satir);
                    eklendi = true;
                }

                // Henüz sonuçlanmamış bir "Bekliyor" satırı: bir sonuç filtresi (satisSonucu)
                // seçiliyse hiçbir zaman eklenmez (bekleyen bir satırın sonucu yoktur). Ayrıca
                // yalnızca gerçekten aktif (süreci devam eden) dosyalarda eklenir - Dosya.Asama,
                // "2. Satış kaydı hiç yoksa 2. Satışı Bekliyor" kısayolunu 1. Satış zaten
                // "Satıldı" sonuçlanıp dosya pasife düşmüş olsa bile döndürür (o özelliğin asıl
                // kullanıldığı yerlerde zaten hep aktif dosyalarla çağrıldığı için sorun
                // çıkarmıyor); burada Satıldı/İade/Kapandı/Terk gibi tamamlanmış bir dosya için
                // yanlışlıkla "sonraki tur bekliyor" satırı eklenmesin diye durum kontrolü eklendi.
                var dosyaAktif = d.Durum == DosyaDurumu.Kayitli || d.Durum == DosyaDurumu.SatisaCikti;
                if (dosyaAktif && !satisSonucu.HasValue && AsamaBekleyenTur.TryGetValue(d.Asama, out var bekleyenTur))
                {
                    if (!satisTuru.HasValue || satisTuru.Value == bekleyenTur)
                    {
                        var planlanmisTarih = bekleyenTur switch
                        {
                            SatisTuru.BirinciSatis => d.Satis1Tarihi,
                            SatisTuru.IkinciSatis => d.Satis2Tarihi,
                            _ => DateTime.Today
                        };
                        if (!satisTarihi.HasValue || satisTarihi.Value.Date == planlanmisTarih.Date)
                        {
                            var satir = DosyaAlanlariniKopyala(d);
                            satir.SatisTuru = bekleyenTur;
                            satir.SatisTarihi = planlanmisTarih;
                            satir.HenuzSonucGirilmedi = true;
                            liste.Add(satir);
                            eklendi = true;
                        }
                    }
                }

                // Hiçbir satış türü/tarihi/sonucu filtresi verilmemişse, her dosya en az bir
                // satırla (bilgisi "-" olsa dahi) listede yer almaya devam eder.
                if (!eklendi && !satisTuru.HasValue && !satisTarihi.HasValue && !satisSonucu.HasValue)
                {
                    liste.Add(DosyaAlanlariniKopyala(d));
                }
            }

            liste = liste
                .OrderByDescending(x => x.DosyaNo)
                .ThenByDescending(x => x.SatisTarihi)
                .ToList();

            // İhale saatini, aynı satış türü/tarihi için kaydedilmiş bir Komisyon İşlemleri
            // oturumundan (varsa) eşleştirerek doldur.
            var komisyonOturumlari = await _db.KomisyonOturumlari.ToListAsync();
            foreach (var satir in liste)
            {
                if (satir.SatisTuru.HasValue && satir.SatisTarihi.HasValue)
                {
                    var eslesen = komisyonOturumlari.FirstOrDefault(k =>
                        k.SatisTuru == satir.SatisTuru.Value && k.SatisTarihi.Date == satir.SatisTarihi.Value.Date);
                    satir.SatisSaati = eslesen?.SatisSaati;
                }
            }

            return liste;
        }

        // Tutanak Türü "İhale Tutanağı" iken toplu dökümde gösterilecek liste: her dosya
        // için gösterilecek satış türü/tarihi, Dosya.Asama'nın (bkz. IhaleTutanagiCozucu)
        // tek kaynak mantığıyla çözülür - dosyanın henüz hiç Satış sonucu girilmemiş
        // (ör. 1. Satış'ı bekleyen) olması burada sorun olmaz, çünkü bu durum zaten
        // Dosya.Satis1Tarihi/Satis2Tarihi üzerinden doğrudan okunur, bir Satış kaydına
        // bağlı değildir. Genel filtreyle (dosyaNo/plaka/vergiDairesi/durum) eşleşen
        // dosyalar arasından, satisTuru/satisTarihi de verilmişse, çözülen tür/tarihe göre
        // ayrıca süzülür.
        private async Task<List<IhaleTutanagiGorunumu>> IhaleGorunumleriGetir(
            string? dosyaNo, string? plaka, string? vergiDairesi, string durum,
            SatisTuru? satisTuruFiltre, DateTime? satisTarihiFiltre)
        {
            var sorgu = _db.Dosyalar.Include(d => d.Satislar).AsQueryable();
            if (!string.IsNullOrWhiteSpace(dosyaNo)) sorgu = sorgu.Where(d => d.DosyaNo == dosyaNo);
            if (!string.IsNullOrWhiteSpace(plaka)) sorgu = sorgu.Where(d => d.Plaka == plaka);
            if (!string.IsNullOrWhiteSpace(vergiDairesi)) sorgu = sorgu.Where(d => d.VergiDairesiAdi == vergiDairesi);
            if (durum == "aktif")
                sorgu = sorgu.Where(d => d.Durum == DosyaDurumu.Kayitli || d.Durum == DosyaDurumu.SatisaCikti);
            else if (durum == "pasif")
                sorgu = sorgu.Where(d => d.Durum == DosyaDurumu.Satildi || d.Durum == DosyaDurumu.IptalEdildi
                    || d.Durum == DosyaDurumu.IadeEdildi || d.Durum == DosyaDurumu.Kapandi
                    || d.Durum == DosyaDurumu.AliciyaTerkEdildi);

            var dosyalar = await sorgu.OrderBy(d => d.DosyaNo).ToListAsync();
            var komisyonOturumlari = await _db.KomisyonOturumlari.ToListAsync();

            var sonuc = new List<IhaleTutanagiGorunumu>();
            foreach (var d in dosyalar)
            {
                var cozum = IhaleTutanagiCozucu.Coz(d);
                if (satisTuruFiltre.HasValue && cozum.Tur != satisTuruFiltre.Value) continue;
                if (satisTarihiFiltre.HasValue && cozum.Tarih.Date != satisTarihiFiltre.Value.Date) continue;

                var oturumlar = komisyonOturumlari.Where(k =>
                    k.SatisTuru == cozum.Tur && k.SatisTarihi.Date == cozum.Tarih.Date).ToList();
                var komisyon = IhaleTutanagiCozucu.KomisyonBilgisi(d, cozum.Tur, cozum.Tarih, oturumlar);

                sonuc.Add(new IhaleTutanagiGorunumu
                {
                    Dosya = d,
                    GosterilecekTur = cozum.Tur,
                    GosterilecekTarih = cozum.Tarih,
                    TarihSeciliyor = cozum.TarihSeciliyor,
                    SatisSaati = komisyon.Saat,
                    KomisyonBaskani = komisyon.Baskan,
                    KomisyonUye1 = komisyon.Uye1,
                    KomisyonUye2 = komisyon.Uye2,
                    KomisyonUye3 = komisyon.Uye3
                });
            }
            return sonuc
                .OrderBy(g => g.GosterilecekTarih)
                .ThenBy(g => g.Dosya.DosyaNo)
                .ToList();
        }

        public async Task<IActionResult> Index(
            string tutanakTuru = "ihale",
            string? dosyaNo = null,
            SatisTuru? satisTuru = null,
            string? plaka = null,
            DateTime? satisTarihi = null,
            string? vergiDairesi = null,
            SatisSonucu? satisSonucu = null,
            string durum = "")
        {
            var veri = await FiltreliListeGetir(dosyaNo, satisTuru, plaka, satisTarihi, vergiDairesi, satisSonucu, durum);

            ViewBag.TutanakTuru = tutanakTuru;
            ViewBag.DosyaNo = dosyaNo;
            ViewBag.SatisTuru = satisTuru;
            ViewBag.Plaka = plaka;
            ViewBag.SatisTarihi = satisTarihi?.ToString("yyyy-MM-dd");
            ViewBag.VergiDairesi = vergiDairesi;
            ViewBag.SatisSonucu = satisSonucu;
            ViewBag.Durum = durum;

            ViewBag.DosyaNolar = await _db.Dosyalar.OrderByDescending(d => d.DosyaNo).Select(d => d.DosyaNo).ToListAsync();
            ViewBag.Plakalar = await _db.Dosyalar.OrderBy(d => d.Plaka).Select(d => d.Plaka).Distinct().ToListAsync();
            ViewBag.VergiDaireleriListesi = await _db.Dosyalar.OrderBy(d => d.VergiDairesiAdi).Select(d => d.VergiDairesiAdi).Distinct().ToListAsync();

            if (tutanakTuru == "ihale")
            {
                ViewBag.IhaleGorunumleri = await IhaleGorunumleriGetir(dosyaNo, plaka, vergiDairesi, durum, satisTuru, satisTarihi);
            }
            else if ((tutanakTuru == "satiskarari" || tutanakTuru == "sorgu") && !string.IsNullOrWhiteSpace(dosyaNo))
            {
                // Bu iki tür, listeden değil doğrudan bir dosya no seçilip tek bir dosya için
                // özet sıra + Görüntüle/Yazdır eylemleriyle çalışır (bkz. Index.cshtml).
                ViewBag.TekilDosya = await _db.Dosyalar.FirstOrDefaultAsync(d => d.DosyaNo == dosyaNo);
            }

            return View(veri);
        }

        // Ekrandaki filtrelenmiş listeyi Excel olarak indirir.
        public async Task<IActionResult> ExcelIndir(
            string? dosyaNo = null, SatisTuru? satisTuru = null, string? plaka = null,
            DateTime? satisTarihi = null, string? vergiDairesi = null, SatisSonucu? satisSonucu = null,
            string durum = "")
        {
            var veri = await FiltreliListeGetir(dosyaNo, satisTuru, plaka, satisTarihi, vergiDairesi, satisSonucu, durum);
            var trTR = new System.Globalization.CultureInfo("tr-TR");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Tutanak ve Raporlar");
            string[] baslik = {
                "Dosya No", "Adı Soyadı/Ünvanı", "Vergi Dairesi", "Alacaklı Vergi Dairesi",
                "VKN", "TCKN", "Plaka", "Cinsi", "Markası", "Model Yılı", "M.Bedel",
                "Satış Türü", "Satış Tarih", "SONUÇ",
                "Komisyon Başkanı", "Komisyon Üye 1", "Komisyon Üye 2", "Komisyon Üye 3"
            };
            for (int i = 0; i < baslik.Length; i++) ws.Cell(1, i + 1).Value = baslik[i];

            var satir = 2;
            foreach (var s in veri)
            {
                ws.Cell(satir, 1).Value = s.DosyaNo;
                ws.Cell(satir, 2).Value = s.AdiSoyadiUnvani;
                ws.Cell(satir, 3).Value = s.VergiDairesiAdi;
                ws.Cell(satir, 4).Value = s.AlacakliVergiDairesi;
                ws.Cell(satir, 5).Value = s.VergiKimlikNumarasi;
                ws.Cell(satir, 6).Value = s.TCKimlikNumarasi;
                ws.Cell(satir, 7).Value = s.Plaka;
                ws.Cell(satir, 8).Value = s.AracCinsi;
                ws.Cell(satir, 9).Value = s.AracMarkasi;
                ws.Cell(satir, 10).Value = s.ModelYili;
                ws.Cell(satir, 11).Value = s.MuhammenBedel;
                ws.Cell(satir, 12).Value = s.SatisTuruGorunen;
                ws.Cell(satir, 13).Value = s.SatisTarihi?.ToString("dd.MM.yyyy") ?? "";
                ws.Cell(satir, 14).Value = s.SonucGorunen;
                ws.Cell(satir, 15).Value = s.KomisyonBaskani;
                ws.Cell(satir, 16).Value = s.KomisyonUye1;
                ws.Cell(satir, 17).Value = s.KomisyonUye2;
                ws.Cell(satir, 18).Value = s.KomisyonUye3;
                satir++;
            }
            ws.Row(1).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"TutanakRapor_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        // Tek bir dosyanın Araç Satış Kararı / Sorgu belgesini Excel olarak indirir
        // (ekrandaki belgeyle aynı alan/değer çiftleri - bkz. DosyaController.ExcelIndir).
        public async Task<IActionResult> SatisKarariExcelIndir(string dosyaNo)
        {
            var veri = await SatisMaliOzetHesaplayici.Getir(_db, dosyaNo);
            if (veri == null || veri.Satis == null || veri.Satis.SatisSonucu != SatisSonucu.Satildi) return NotFound();
            var d = veri.Dosya;
            var satis = veri.Satis;
            var trTR = new System.Globalization.CultureInfo("tr-TR");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Araç Satış Kararı");
            var satir = 1;
            void Yaz(string alan, object? deger)
            {
                ws.Cell(satir, 1).Value = alan;
                ws.Cell(satir, 1).Style.Font.Bold = true;
                ws.Cell(satir, 2).Value = deger?.ToString() ?? "";
                satir++;
            }
            Yaz("Dosya No", d.DosyaNo);
            Yaz("Adı Soyadı/Ünvanı", d.AdiSoyadiUnvani);
            Yaz("Vergi Numarası", d.VergiKimlikNumarasi);
            Yaz("TC Kimlik Numarası", d.TCKimlikNumarasi);
            Yaz("Vergi Dairesi Adı", d.VergiDairesiAdi);
            Yaz("Alacaklı Vergi Dairesi", d.AlacakliVergiDairesi);
            Yaz("Plaka", d.Plaka);
            Yaz("Cinsi", d.AracCinsi);
            Yaz("Markası", d.AracMarkasi);
            Yaz("Tipi", d.AracTipi);
            Yaz("Model Yılı", d.ModelYili);
            Yaz("Renk", d.Renk);
            Yaz("Muhammen Bedeli (TL)", d.MuhammenBedel);
            Yaz("Satış Bedeli (TL)", veri.SatisBedeli);
            Yaz("KDV Tutarı (TL)", veri.KdvTutari);
            Yaz("Damga Vergisi Tutarı (TL)", veri.DvTutari);
            Yaz("Toplam Tutar (TL)", veri.ToplamTutar);
            Yaz("Satış Türü", satis.SatisTuruGorunen);
            Yaz("Satış Tarihi", satis.SatisTarihi.ToString("dd.MM.yyyy"));
            Yaz("Alıcı Adı Soyadı", satis.AliciAdiSoyadi);
            Yaz("Alıcı Adresi", satis.Adres);
            Yaz("Alıcı Vergi Kimlik No", satis.AliciVKN);
            Yaz("Alıcı TC Kimlik No", satis.AliciTCKN);
            Yaz("Vade Tarihi", satis.VerilenSure?.ToString("dd.MM.yyyy"));
            Yaz("Faturayı Kesecek Otopark", satis.FaturayiKesenOtopark);
            Yaz("Çekici Bedeli (TL)", satis.CekiciBedeli);
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"AracSatisKarari_{dosyaNo.Replace("/", "_")}.xlsx");
        }

        // Tek bir dosyanın Sorgu belgesini Excel olarak indirir.
        public async Task<IActionResult> SorguExcelIndir(string dosyaNo)
        {
            var dosya = await _db.Dosyalar.Include(d => d.Satislar).FirstOrDefaultAsync(d => d.DosyaNo == dosyaNo);
            if (dosya == null) return NotFound();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Sorgu");
            var satir = 1;
            void Yaz(string alan, object? deger)
            {
                ws.Cell(satir, 1).Value = alan;
                ws.Cell(satir, 1).Style.Font.Bold = true;
                ws.Cell(satir, 2).Value = deger?.ToString() ?? "";
                satir++;
            }
            Yaz("Dosya No", dosya.DosyaNo);
            Yaz("Kayıt Tarihi", dosya.KayitTarihi.ToString("dd.MM.yyyy"));
            Yaz("Adı Soyadı/Ünvanı", dosya.AdiSoyadiUnvani);
            Yaz("Vergi Dairesi Adı", dosya.VergiDairesiAdi);
            Yaz("Alacaklı Vergi Dairesi", dosya.AlacakliVergiDairesi);
            Yaz("Plaka", dosya.Plaka);
            Yaz("Markası", dosya.AracMarkasi);
            Yaz("Cinsi", dosya.AracCinsi);
            Yaz("Muhammen Bedel (TL)", dosya.MuhammenBedel);
            Yaz("Durum", dosya.GenelDurumGorunen);
            satir++;
            ws.Cell(satir, 1).Value = "SATIŞ GEÇMİŞİ";
            ws.Cell(satir, 1).Style.Font.Bold = true;
            satir++;
            string[] altBaslik = { "Satış Türü", "Satış Tarihi", "Sonuç", "Satış Bedeli", "Alıcı" };
            for (int i = 0; i < altBaslik.Length; i++) ws.Cell(satir, i + 1).Value = altBaslik[i];
            ws.Row(satir).Style.Font.Bold = true;
            satir++;
            foreach (var s in dosya.Satislar.OrderBy(s => s.SatisTarihi))
            {
                ws.Cell(satir, 1).Value = s.SatisTuruGorunen;
                ws.Cell(satir, 2).Value = s.SatisTarihi.ToString("dd.MM.yyyy");
                ws.Cell(satir, 3).Value = s.SonucGorunen;
                ws.Cell(satir, 4).Value = s.SatisBedeli;
                ws.Cell(satir, 5).Value = s.AliciAdiSoyadi;
                satir++;
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Sorgu_{dosyaNo.Replace("/", "_")}.xlsx");
        }

        // GET: /TutanakRapor/SatisKarari?dosyaNo=.. - "Satış Kararı" türü için tekil belge.
        // Yalnızca o dosyanın "Satıldı" sonuçlanmış bir satışı varsa anlamlıdır (bu belge
        // gerçekleşmiş bir satışın resmi kararını gösterir); yoksa listeye geri döner.
        public async Task<IActionResult> SatisKarari(string dosyaNo)
        {
            var veri = await SatisMaliOzetHesaplayici.Getir(_db, dosyaNo);
            if (veri == null || veri.Satis == null || veri.Satis.SatisSonucu != SatisSonucu.Satildi)
            {
                TempData["Hata"] = $"\"{dosyaNo}\" numaralı dosya için \"Satıldı\" sonuçlanmış bir satış bulunamadı. Araç Satış Kararı yalnızca tamamlanmış satışlar için düzenlenebilir.";
                return RedirectToAction(nameof(Index), new { tutanakTuru = "satiskarari" });
            }

            // NOT: Satis kaydı yalnızca tarih tutar, saat tutmaz; aynı gün birden fazla
            // Pazarlık/6183-86 oturumu varsa burada hangisinin bu satışa ait olduğu kesin
            // ayırt edilemez (bkz. IhaleTutanagiCozucu.KomisyonBilgisi dokümantasyonu) - bu
            // durumda o günün ilk kaydedilen oturumu gösterilir.
            var oturumlar = await _db.KomisyonOturumlari
                .Where(k => k.SatisTuru == veri.Satis.SatisTuru && k.SatisTarihi.Date == veri.Satis.SatisTarihi.Date)
                .ToListAsync();
            var komisyon = IhaleTutanagiCozucu.KomisyonBilgisi(veri.Dosya, veri.Satis.SatisTuru, veri.Satis.SatisTarihi, oturumlar);

            ViewBag.KomisyonBaskani = komisyon.Baskan;
            ViewBag.KomisyonUye1 = komisyon.Uye1;
            ViewBag.KomisyonUye2 = komisyon.Uye2;
            ViewBag.KomisyonUye3 = komisyon.Uye3;
            ViewBag.SatisSaati = komisyon.Saat;

            return View(veri);
        }

        // GET: /TutanakRapor/Sorgu?dosyaNo=.. - "Sorgu" türü için tekil dosya özeti belgesi.
        // Satış sonucu olsun olmasın (henüz hiç satılmamış bir dosya için de) kullanılabilir.
        public async Task<IActionResult> Sorgu(string dosyaNo)
        {
            var dosya = await _db.Dosyalar.Include(d => d.Satislar).FirstOrDefaultAsync(d => d.DosyaNo == dosyaNo);
            if (dosya == null)
            {
                TempData["Hata"] = $"\"{dosyaNo}\" numaralı dosya bulunamadı.";
                return RedirectToAction(nameof(Index), new { tutanakTuru = "sorgu" });
            }
            ViewBag.GenelAyar = await _db.GenelAyarlar.FirstOrDefaultAsync() ?? new GenelAyar();
            ViewBag.OtoparkKategoriSozlugu = await _db.AracCinsleri.ToDictionaryAsync(c => c.Ad, c => c.OtoparkKategorisi);
            return View(dosya);
        }
    }
}
