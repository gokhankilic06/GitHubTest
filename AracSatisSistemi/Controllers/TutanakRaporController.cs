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
                    || x.d.Durum == DosyaDurumu.IadeEdildi || x.d.Durum == DosyaDurumu.Kapandi);

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
    }
}
