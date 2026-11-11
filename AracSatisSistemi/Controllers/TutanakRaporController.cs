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
            public string SonucGorunen => SatisSonucu switch
            {
                AracSatisSistemi.Models.SatisSonucu.Satildi => "Satıldı",
                AracSatisSistemi.Models.SatisSonucu.AliciCikmadi => "Alıcı Çıkmadı",
                AracSatisSistemi.Models.SatisSonucu.AliciIslemYapmadi => "Alıcı İşlem Yapmadı",
                _ => "-"
            };
        }

        private async Task<List<SatirSonuc>> FiltreliListeGetir(
            string? dosyaNo, SatisTuru? satisTuru, string? plaka, DateTime? satisTarihi,
            string? vergiDairesi, SatisSonucu? satisSonucu, string durum)
        {
            var sorgu =
                from d in _db.Dosyalar
                join s in _db.Satislar on d.Id equals s.DosyaId into satislar
                from s in satislar.DefaultIfEmpty()
                select new { d, s };

            if (!string.IsNullOrWhiteSpace(dosyaNo)) sorgu = sorgu.Where(x => x.d.DosyaNo == dosyaNo);
            if (!string.IsNullOrWhiteSpace(plaka)) sorgu = sorgu.Where(x => x.d.Plaka == plaka);
            if (!string.IsNullOrWhiteSpace(vergiDairesi)) sorgu = sorgu.Where(x => x.d.VergiDairesiAdi == vergiDairesi);
            if (satisTuru.HasValue) sorgu = sorgu.Where(x => x.s != null && x.s.SatisTuru == satisTuru.Value);
            if (satisTarihi.HasValue) sorgu = sorgu.Where(x => x.s != null && x.s.SatisTarihi.Date == satisTarihi.Value.Date);
            if (satisSonucu.HasValue) sorgu = sorgu.Where(x => x.s != null && x.s.SatisSonucu == satisSonucu.Value);

            if (durum == "aktif")
                sorgu = sorgu.Where(x => x.d.Durum == DosyaDurumu.Kayitli || x.d.Durum == DosyaDurumu.SatisaCikti);
            else if (durum == "pasif")
                sorgu = sorgu.Where(x => x.d.Durum == DosyaDurumu.Satildi || x.d.Durum == DosyaDurumu.IptalEdildi
                    || x.d.Durum == DosyaDurumu.IadeEdildi || x.d.Durum == DosyaDurumu.Kapandi
                    || x.d.Durum == DosyaDurumu.AliciyaTerkEdildi);

            var liste = await sorgu
                .OrderByDescending(x => x.d.DosyaNo)
                .ThenByDescending(x => x.s != null ? x.s.SatisTarihi : (DateTime?)null)
                .Select(x => new SatirSonuc
                {
                    DosyaId = x.d.Id,
                    DosyaNo = x.d.DosyaNo,
                    Plaka = x.d.Plaka,
                    AracCinsi = x.d.AracCinsi,
                    AracTipi = x.d.AracTipi,
                    AracMarkasi = x.d.AracMarkasi,
                    ModelYili = x.d.ModelYili,
                    MuhammenBedel = x.d.MuhammenBedel,
                    SatisTuru = x.s != null ? x.s.SatisTuru : (SatisTuru?)null,
                    SatisTarihi = x.s != null ? x.s.SatisTarihi : (DateTime?)null,
                    SatisSonucu = x.s != null ? x.s.SatisSonucu : (SatisSonucu?)null,
                    AdiSoyadiUnvani = x.d.AdiSoyadiUnvani,
                    VergiKimlikNumarasi = x.d.VergiKimlikNumarasi,
                    TCKimlikNumarasi = x.d.TCKimlikNumarasi,
                    VergiDairesiAdi = x.d.VergiDairesiAdi,
                    AlacakliVergiDairesi = x.d.AlacakliVergiDairesi,
                    KomisyonBaskani = x.d.KomisyonBaskani,
                    KomisyonUye1 = x.d.KomisyonUye1,
                    KomisyonUye2 = x.d.KomisyonUye2,
                    KomisyonUye3 = x.d.KomisyonUye3
                })
                .ToListAsync();

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

                var oturum = komisyonOturumlari.FirstOrDefault(k =>
                    k.SatisTuru == cozum.Tur && k.SatisTarihi.Date == cozum.Tarih.Date);
                var komisyon = IhaleTutanagiCozucu.KomisyonBilgisi(d, cozum.Tur, cozum.Tarih, oturum);

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
            string durum = "aktif")
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
            string durum = "aktif")
        {
            var veri = await FiltreliListeGetir(dosyaNo, satisTuru, plaka, satisTarihi, vergiDairesi, satisSonucu, durum);
            var trTR = new System.Globalization.CultureInfo("tr-TR");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Tutanak ve Raporlar");
            string[] baslik = { "Dosya No", "Plaka", "Cinsi", "Markası", "M.Bedel", "Satış Türü", "Satış Tarih", "SONUÇ" };
            for (int i = 0; i < baslik.Length; i++) ws.Cell(1, i + 1).Value = baslik[i];

            var satir = 2;
            foreach (var s in veri)
            {
                ws.Cell(satir, 1).Value = s.DosyaNo;
                ws.Cell(satir, 2).Value = s.Plaka;
                ws.Cell(satir, 3).Value = s.AracCinsi;
                ws.Cell(satir, 4).Value = s.AracMarkasi;
                ws.Cell(satir, 5).Value = s.MuhammenBedel;
                ws.Cell(satir, 6).Value = s.SatisTuruGorunen;
                ws.Cell(satir, 7).Value = s.SatisTarihi?.ToString("dd.MM.yyyy") ?? "";
                ws.Cell(satir, 8).Value = s.SonucGorunen;
                satir++;
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"TutanakRapor_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
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

            var oturum = await _db.KomisyonOturumlari.FirstOrDefaultAsync(k =>
                k.SatisTuru == veri.Satis.SatisTuru && k.SatisTarihi.Date == veri.Satis.SatisTarihi.Date);
            var komisyon = IhaleTutanagiCozucu.KomisyonBilgisi(veri.Dosya, veri.Satis.SatisTuru, veri.Satis.SatisTarihi, oturum);

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
            return View(dosya);
        }
    }
}
