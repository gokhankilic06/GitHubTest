using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;
using ClosedXML.Excel;

namespace AracSatisSistemi.Controllers
{
    // İSTATİSTİK ekranı: tüm dosya/satış kayıtlarının yıl, ay, tarih aralığı, satış türü,
    // satış sonucu, dosya durumu ve vergi dairesine göre filtrelenebildiği; filtrelenen
    // kayıtlar üzerinden toplam/ortalama tutarlar ile yıl/tür/sonuç bazlı kırılımların
    // otomatik hesaplandığı tek ekran.
    public class IstatistikController : Controller
    {
        private readonly AppDbContext _db;
        public IstatistikController(AppDbContext db) => _db = db;

        public class Satir
        {
            public int DosyaId { get; set; }
            public string DosyaNo { get; set; } = string.Empty;
            public string Plaka { get; set; } = string.Empty;
            public string AracCinsi { get; set; } = string.Empty;
            public string AracMarkasi { get; set; } = string.Empty;
            public string AdiSoyadiUnvani { get; set; } = string.Empty;
            public string VergiDairesiAdi { get; set; } = string.Empty;
            public string AlacakliVergiDairesi { get; set; } = string.Empty;
            public string Il { get; set; } = string.Empty;
            public string? OtoparkAdi { get; set; }
            public DateTime KayitTarihi { get; set; }
            public decimal MuhammenBedel { get; set; }
            public DosyaDurumu Durum { get; set; }
            public string DurumGorunen { get; set; } = string.Empty;
            public SatisTuru? SatisTuru { get; set; }
            public DateTime? SatisTarihi { get; set; }
            public SatisSonucu? SatisSonucu { get; set; }
            public decimal? SatisBedeli { get; set; }
            public decimal KdvTutari { get; set; }
            public decimal DvTutari { get; set; }
            public decimal OtoparkTutari { get; set; }
            public decimal ToplamTutar { get; set; }

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

        public class Kirilim
        {
            public string Etiket { get; set; } = string.Empty;
            public int DosyaSayisi { get; set; }
            public int SatisSayisi { get; set; }
            public int SatilanSayisi { get; set; }
            public decimal ToplamSatisBedeli { get; set; }
        }

        public class IstatistikModeli
        {
            public List<Satir> Satirlar { get; set; } = new();

            public int ToplamDosyaSayisi { get; set; }
            public int ToplamSatisKaydiSayisi { get; set; }
            public int SatilanSayisi { get; set; }
            public int AliciCikmadiSayisi { get; set; }
            public int AliciIslemYapmadiSayisi { get; set; }
            public decimal ToplamSatisBedeli { get; set; }
            public decimal OrtalamaSatisBedeli { get; set; }
            public decimal ToplamKdv { get; set; }
            public decimal ToplamDv { get; set; }
            public decimal ToplamOtopark { get; set; }

            public List<Kirilim> YilKirilimi { get; set; } = new();
            public List<Kirilim> SatisTuruKirilimi { get; set; } = new();
            public List<Kirilim> SonucKirilimi { get; set; } = new();
            public List<Kirilim> VergiDairesiKirilimi { get; set; } = new();
            public List<Kirilim> IlKirilimi { get; set; } = new();
            public List<Kirilim> AracCinsiKirilimi { get; set; } = new();
            public List<Kirilim> AracMarkasiKirilimi { get; set; } = new();
        }

        private async Task<List<Satir>> FiltreliListeGetir(
            int? yil, int? ay, DateTime? baslangic, DateTime? bitis,
            SatisTuru? satisTuru, SatisSonucu? satisSonucu, DosyaDurumu? durum, string? vergiDairesi,
            string? alacakliVergiDairesi, string? il, string? aracCinsi, string? aracMarkasi,
            string? otoparkAdi, string? dosyaNo, string? plaka)
        {
            var sorgu =
                from d in _db.Dosyalar
                join s in _db.Satislar on d.Id equals s.DosyaId into satislar
                from s in satislar.DefaultIfEmpty()
                select new { d, s };

            if (yil.HasValue)
                sorgu = sorgu.Where(x => x.d.KayitTarihi.Year == yil.Value || (x.s != null && x.s.SatisTarihi.Year == yil.Value));
            if (ay.HasValue)
                sorgu = sorgu.Where(x => x.d.KayitTarihi.Month == ay.Value || (x.s != null && x.s.SatisTarihi.Month == ay.Value));
            if (baslangic.HasValue)
                sorgu = sorgu.Where(x => x.s != null && x.s.SatisTarihi.Date >= baslangic.Value.Date);
            if (bitis.HasValue)
                sorgu = sorgu.Where(x => x.s != null && x.s.SatisTarihi.Date <= bitis.Value.Date);
            if (satisTuru.HasValue)
                sorgu = sorgu.Where(x => x.s != null && x.s.SatisTuru == satisTuru.Value);
            if (satisSonucu.HasValue)
                sorgu = sorgu.Where(x => x.s != null && x.s.SatisSonucu == satisSonucu.Value);
            if (durum.HasValue)
                sorgu = sorgu.Where(x => x.d.Durum == durum.Value);
            if (!string.IsNullOrWhiteSpace(vergiDairesi))
                sorgu = sorgu.Where(x => x.d.VergiDairesiAdi == vergiDairesi);
            if (!string.IsNullOrWhiteSpace(alacakliVergiDairesi))
                sorgu = sorgu.Where(x => x.d.AlacakliVergiDairesi == alacakliVergiDairesi);
            if (!string.IsNullOrWhiteSpace(aracCinsi))
                sorgu = sorgu.Where(x => x.d.AracCinsi == aracCinsi);
            if (!string.IsNullOrWhiteSpace(aracMarkasi))
                sorgu = sorgu.Where(x => x.d.AracMarkasi == aracMarkasi);
            if (!string.IsNullOrWhiteSpace(otoparkAdi))
                sorgu = sorgu.Where(x => x.d.OtoparkAdi == otoparkAdi);
            if (!string.IsNullOrWhiteSpace(dosyaNo))
                sorgu = sorgu.Where(x => x.d.DosyaNo.Contains(dosyaNo));
            if (!string.IsNullOrWhiteSpace(plaka))
                sorgu = sorgu.Where(x => x.d.Plaka.Contains(plaka));
            if (!string.IsNullOrWhiteSpace(il))
            {
                var ilVergiDaireleri = await _db.VergiDaireleri.Where(v => v.Sehir == il).Select(v => v.Ad).ToListAsync();
                sorgu = sorgu.Where(x => ilVergiDaireleri.Contains(x.d.VergiDairesiAdi));
            }

            var genelAyar = await _db.GenelAyarlar.FirstOrDefaultAsync() ?? new GenelAyar();
            var kategoriSozlugu = await _db.AracCinsleri.ToDictionaryAsync(c => c.Ad, c => c.OtoparkKategorisi);
            var sehirSozlugu = (await _db.VergiDaireleri.ToListAsync())
                .GroupBy(v => v.Ad).ToDictionary(g => g.Key, g => g.First().Sehir);

            var liste = await sorgu
                .OrderByDescending(x => x.d.DosyaNo)
                .ThenByDescending(x => x.s != null ? x.s.SatisTarihi : (DateTime?)null)
                .ToListAsync();

            return liste.Select(x =>
            {
                var satisBedeli = x.s?.SatisBedeli;
                var kdv = satisBedeli.HasValue ? Math.Round(satisBedeli.Value * x.d.KdvOrani / 100m, 2) : 0m;
                var dv = satisBedeli.HasValue ? Math.Round(satisBedeli.Value * x.d.DamgaVergisiOrani / 1000m, 2) : 0m;
                var otopark = x.s != null
                    ? OtoparkHesaplayici.GercekOtoparkUcreti(x.s, x.d.AracCinsi, kategoriSozlugu, genelAyar)
                    : 0m;
                var toplam = (satisBedeli ?? 0m) + kdv + dv;

                return new Satir
                {
                    DosyaId = x.d.Id,
                    DosyaNo = x.d.DosyaNo,
                    Plaka = x.d.Plaka,
                    AracCinsi = x.d.AracCinsi,
                    AracMarkasi = x.d.AracMarkasi,
                    AdiSoyadiUnvani = x.d.AdiSoyadiUnvani,
                    VergiDairesiAdi = x.d.VergiDairesiAdi,
                    AlacakliVergiDairesi = x.d.AlacakliVergiDairesi,
                    Il = sehirSozlugu.TryGetValue(x.d.VergiDairesiAdi, out var sehir) ? sehir : "-",
                    OtoparkAdi = x.d.OtoparkAdi,
                    KayitTarihi = x.d.KayitTarihi,
                    MuhammenBedel = x.d.MuhammenBedel,
                    Durum = x.d.Durum,
                    DurumGorunen = x.d.DurumGorunenAd,
                    SatisTuru = x.s?.SatisTuru,
                    SatisTarihi = x.s?.SatisTarihi,
                    SatisSonucu = x.s?.SatisSonucu,
                    SatisBedeli = satisBedeli,
                    KdvTutari = kdv,
                    DvTutari = dv,
                    OtoparkTutari = otopark,
                    ToplamTutar = toplam
                };
            }).ToList();
        }

        private static IstatistikModeli ModeliOlustur(List<Satir> satirlar)
        {
            var satilanlar = satirlar.Where(s => s.SatisSonucu == SatisSonucu.Satildi).ToList();

            var model = new IstatistikModeli
            {
                Satirlar = satirlar,
                ToplamDosyaSayisi = satirlar.Select(s => s.DosyaId).Distinct().Count(),
                ToplamSatisKaydiSayisi = satirlar.Count(s => s.SatisTuru.HasValue),
                SatilanSayisi = satilanlar.Count,
                AliciCikmadiSayisi = satirlar.Count(s => s.SatisSonucu == SatisSonucu.AliciCikmadi),
                AliciIslemYapmadiSayisi = satirlar.Count(s => s.SatisSonucu == SatisSonucu.AliciIslemYapmadi),
                ToplamSatisBedeli = satilanlar.Sum(s => s.SatisBedeli ?? 0m),
                ToplamKdv = satilanlar.Sum(s => s.KdvTutari),
                ToplamDv = satilanlar.Sum(s => s.DvTutari),
                ToplamOtopark = satilanlar.Sum(s => s.OtoparkTutari)
            };
            model.OrtalamaSatisBedeli = satilanlar.Count > 0 ? Math.Round(model.ToplamSatisBedeli / satilanlar.Count, 2) : 0m;

            model.YilKirilimi = satirlar
                .Select(s => s.SatisTarihi?.Year ?? s.KayitTarihi.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .Select(yil => new Kirilim
                {
                    Etiket = yil.ToString(),
                    DosyaSayisi = satirlar.Where(s => (s.SatisTarihi?.Year ?? s.KayitTarihi.Year) == yil).Select(s => s.DosyaId).Distinct().Count(),
                    SatisSayisi = satirlar.Count(s => (s.SatisTarihi?.Year ?? s.KayitTarihi.Year) == yil && s.SatisTuru.HasValue),
                    SatilanSayisi = satirlar.Count(s => (s.SatisTarihi?.Year ?? s.KayitTarihi.Year) == yil && s.SatisSonucu == SatisSonucu.Satildi),
                    ToplamSatisBedeli = satirlar.Where(s => (s.SatisTarihi?.Year ?? s.KayitTarihi.Year) == yil && s.SatisSonucu == SatisSonucu.Satildi).Sum(s => s.SatisBedeli ?? 0m)
                }).ToList();

            model.SatisTuruKirilimi = satirlar
                .Where(s => s.SatisTuru.HasValue)
                .GroupBy(s => s.SatisTuru!.Value)
                .Select(g => new Kirilim
                {
                    Etiket = g.First().SatisTuruGorunen,
                    DosyaSayisi = g.Select(s => s.DosyaId).Distinct().Count(),
                    SatisSayisi = g.Count(),
                    SatilanSayisi = g.Count(s => s.SatisSonucu == SatisSonucu.Satildi),
                    ToplamSatisBedeli = g.Where(s => s.SatisSonucu == SatisSonucu.Satildi).Sum(s => s.SatisBedeli ?? 0m)
                }).ToList();

            model.SonucKirilimi = satirlar
                .Where(s => s.SatisSonucu.HasValue)
                .GroupBy(s => s.SatisSonucu!.Value)
                .Select(g => new Kirilim
                {
                    Etiket = g.First().SonucGorunen,
                    DosyaSayisi = g.Select(s => s.DosyaId).Distinct().Count(),
                    SatisSayisi = g.Count(),
                    SatilanSayisi = g.Count(s => s.SatisSonucu == SatisSonucu.Satildi),
                    ToplamSatisBedeli = g.Where(s => s.SatisSonucu == SatisSonucu.Satildi).Sum(s => s.SatisBedeli ?? 0m)
                }).ToList();

            model.VergiDairesiKirilimi = satirlar
                .GroupBy(s => s.VergiDairesiAdi)
                .Select(g => new Kirilim
                {
                    Etiket = g.Key,
                    DosyaSayisi = g.Select(s => s.DosyaId).Distinct().Count(),
                    SatisSayisi = g.Count(s => s.SatisTuru.HasValue),
                    SatilanSayisi = g.Count(s => s.SatisSonucu == SatisSonucu.Satildi),
                    ToplamSatisBedeli = g.Where(s => s.SatisSonucu == SatisSonucu.Satildi).Sum(s => s.SatisBedeli ?? 0m)
                })
                .OrderByDescending(k => k.DosyaSayisi)
                .ToList();

            model.IlKirilimi = satirlar
                .GroupBy(s => s.Il)
                .Select(g => new Kirilim
                {
                    Etiket = g.Key,
                    DosyaSayisi = g.Select(s => s.DosyaId).Distinct().Count(),
                    SatisSayisi = g.Count(s => s.SatisTuru.HasValue),
                    SatilanSayisi = g.Count(s => s.SatisSonucu == SatisSonucu.Satildi),
                    ToplamSatisBedeli = g.Where(s => s.SatisSonucu == SatisSonucu.Satildi).Sum(s => s.SatisBedeli ?? 0m)
                })
                .OrderByDescending(k => k.DosyaSayisi)
                .ToList();

            model.AracCinsiKirilimi = satirlar
                .GroupBy(s => s.AracCinsi)
                .Select(g => new Kirilim
                {
                    Etiket = g.Key,
                    DosyaSayisi = g.Select(s => s.DosyaId).Distinct().Count(),
                    SatisSayisi = g.Count(s => s.SatisTuru.HasValue),
                    SatilanSayisi = g.Count(s => s.SatisSonucu == SatisSonucu.Satildi),
                    ToplamSatisBedeli = g.Where(s => s.SatisSonucu == SatisSonucu.Satildi).Sum(s => s.SatisBedeli ?? 0m)
                })
                .OrderByDescending(k => k.DosyaSayisi)
                .ToList();

            model.AracMarkasiKirilimi = satirlar
                .GroupBy(s => s.AracMarkasi)
                .Select(g => new Kirilim
                {
                    Etiket = g.Key,
                    DosyaSayisi = g.Select(s => s.DosyaId).Distinct().Count(),
                    SatisSayisi = g.Count(s => s.SatisTuru.HasValue),
                    SatilanSayisi = g.Count(s => s.SatisSonucu == SatisSonucu.Satildi),
                    ToplamSatisBedeli = g.Where(s => s.SatisSonucu == SatisSonucu.Satildi).Sum(s => s.SatisBedeli ?? 0m)
                })
                .OrderByDescending(k => k.DosyaSayisi)
                .Take(15)
                .ToList();

            return model;
        }

        public async Task<IActionResult> Index(
            int? yil, int? ay, DateTime? baslangic, DateTime? bitis,
            SatisTuru? satisTuru, SatisSonucu? satisSonucu, DosyaDurumu? durum, string? vergiDairesi,
            string? alacakliVergiDairesi, string? il, string? aracCinsi, string? aracMarkasi,
            string? otoparkAdi, string? dosyaNo, string? plaka)
        {
            var satirlar = await FiltreliListeGetir(yil, ay, baslangic, bitis, satisTuru, satisSonucu, durum, vergiDairesi,
                alacakliVergiDairesi, il, aracCinsi, aracMarkasi, otoparkAdi, dosyaNo, plaka);
            var model = ModeliOlustur(satirlar);

            ViewBag.Yil = yil;
            ViewBag.Ay = ay;
            ViewBag.Baslangic = baslangic?.ToString("yyyy-MM-dd");
            ViewBag.Bitis = bitis?.ToString("yyyy-MM-dd");
            ViewBag.SatisTuru = satisTuru;
            ViewBag.SatisSonucu = satisSonucu;
            ViewBag.Durum = durum;
            ViewBag.VergiDairesi = vergiDairesi;
            ViewBag.AlacakliVergiDairesi = alacakliVergiDairesi;
            ViewBag.Il = il;
            ViewBag.AracCinsi = aracCinsi;
            ViewBag.AracMarkasi = aracMarkasi;
            ViewBag.OtoparkAdi = otoparkAdi;
            ViewBag.DosyaNo = dosyaNo;
            ViewBag.Plaka = plaka;

            var yillar = await _db.Dosyalar.Select(d => d.KayitTarihi.Year)
                .Union(_db.Satislar.Select(s => s.SatisTarihi.Year))
                .Distinct().OrderByDescending(y => y).ToListAsync();
            ViewBag.Yillar = yillar;
            ViewBag.VergiDaireleriListesi = await _db.Dosyalar.OrderBy(d => d.VergiDairesiAdi).Select(d => d.VergiDairesiAdi).Distinct().ToListAsync();
            ViewBag.AlacakliVergiDaireleriListesi = await _db.Dosyalar.OrderBy(d => d.AlacakliVergiDairesi).Select(d => d.AlacakliVergiDairesi).Distinct().ToListAsync();
            ViewBag.IllerListesi = await _db.VergiDaireleri.OrderBy(v => v.Sehir).Select(v => v.Sehir).Distinct().ToListAsync();
            ViewBag.AracCinsleriListesi = await _db.Dosyalar.OrderBy(d => d.AracCinsi).Select(d => d.AracCinsi).Distinct().ToListAsync();
            ViewBag.AracMarkalariListesi = await _db.Dosyalar.OrderBy(d => d.AracMarkasi).Select(d => d.AracMarkasi).Distinct().ToListAsync();
            ViewBag.OtoparklarListesi = await _db.Dosyalar.Where(d => d.OtoparkAdi != null).OrderBy(d => d.OtoparkAdi).Select(d => d.OtoparkAdi).Distinct().ToListAsync();

            return View(model);
        }

        public async Task<IActionResult> ExcelIndir(
            int? yil, int? ay, DateTime? baslangic, DateTime? bitis,
            SatisTuru? satisTuru, SatisSonucu? satisSonucu, DosyaDurumu? durum, string? vergiDairesi,
            string? alacakliVergiDairesi, string? il, string? aracCinsi, string? aracMarkasi,
            string? otoparkAdi, string? dosyaNo, string? plaka)
        {
            var satirlar = await FiltreliListeGetir(yil, ay, baslangic, bitis, satisTuru, satisSonucu, durum, vergiDairesi,
                alacakliVergiDairesi, il, aracCinsi, aracMarkasi, otoparkAdi, dosyaNo, plaka);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("İstatistik");
            string[] baslik = { "Dosya No", "Plaka", "Cinsi", "Markası", "Adı Soyadı/Ünvanı", "Vergi Dairesi", "Alacaklı Vergi Dairesi", "İl", "Durum",
                "Satış Türü", "Satış Tarihi", "Sonuç", "Satış Bedeli", "KDV", "Damga Vergisi", "Otopark", "Toplam Tutar" };
            for (int i = 0; i < baslik.Length; i++) ws.Cell(1, i + 1).Value = baslik[i];

            var satir = 2;
            foreach (var s in satirlar)
            {
                ws.Cell(satir, 1).Value = s.DosyaNo;
                ws.Cell(satir, 2).Value = s.Plaka;
                ws.Cell(satir, 3).Value = s.AracCinsi;
                ws.Cell(satir, 4).Value = s.AracMarkasi;
                ws.Cell(satir, 5).Value = s.AdiSoyadiUnvani;
                ws.Cell(satir, 6).Value = s.VergiDairesiAdi;
                ws.Cell(satir, 7).Value = s.AlacakliVergiDairesi;
                ws.Cell(satir, 8).Value = s.Il;
                ws.Cell(satir, 9).Value = s.DurumGorunen;
                ws.Cell(satir, 10).Value = s.SatisTuruGorunen;
                ws.Cell(satir, 11).Value = s.SatisTarihi?.ToString("dd.MM.yyyy") ?? "";
                ws.Cell(satir, 12).Value = s.SonucGorunen;
                ws.Cell(satir, 13).Value = s.SatisBedeli;
                ws.Cell(satir, 14).Value = s.KdvTutari;
                ws.Cell(satir, 15).Value = s.DvTutari;
                ws.Cell(satir, 16).Value = s.OtoparkTutari;
                ws.Cell(satir, 17).Value = s.ToplamTutar;
                satir++;
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Istatistik_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }
    }
}
