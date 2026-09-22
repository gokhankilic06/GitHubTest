using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;
using ClosedXML.Excel;

namespace AracSatisSistemi.Controllers
{
    public class DosyaController : Controller
    {
        private readonly AppDbContext _db;
        public DosyaController(AppDbContext db) => _db = db;

        // GET: /Dosya  -> dosya arama / listeleme
        public async Task<IActionResult> Index(string? arama)
        {
            var sorgu = _db.Dosyalar.AsQueryable();
            if (!string.IsNullOrWhiteSpace(arama))
            {
                sorgu = sorgu.Where(d => d.DosyaNo.Contains(arama)
                    || d.Plaka.Contains(arama)
                    || d.AdiSoyadiUnvani.Contains(arama));
            }
            ViewBag.Arama = arama;
            var liste = await sorgu.OrderByDescending(d => d.KayitTarihi).ToListAsync();
            return View(liste);
        }

        // GET: /Dosya/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var dosya = await _db.Dosyalar
                .Include(d => d.Satislar)
                .Include(d => d.IptalIade)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (dosya == null) return NotFound();
            return View(dosya);
        }

        // GET: /Dosya/Create
        public async Task<IActionResult> Create()
        {
            await DoldurListeler();
            var komisyon = await _db.KomisyonAyarlari.FirstOrDefaultAsync();
            var dosya = new Dosya
            {
                ModelYili = DateTime.Now.Year,
                KomisyonBaskani = komisyon?.Baskan,
                KomisyonUye1 = komisyon?.Uye1,
                KomisyonUye2 = komisyon?.Uye2,
                KomisyonUye3 = komisyon?.Uye3,
                KomisyonUye4 = komisyon?.Uye4
            };
            return View(dosya);
        }

        // POST: /Dosya/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Dosya dosya,
            DateTime? satis1Tarihi, SatisSonucu? satis1Sonuc, decimal? satis1Bedel, string? satis1Alici,
            DateTime? satis2Tarihi, SatisSonucu? satis2Sonuc, decimal? satis2Bedel, string? satis2Alici)
        {
            if (!ModelState.IsValid)
            {
                await DoldurListeler();
                return View(dosya);
            }

            dosya.DosyaNo = SeedData.YeniDosyaNoUret(_db);
            dosya.KayitTarihi = DateTime.Now;
            dosya.Durum = DosyaDurumu.Kayitli;

            // Dosyanın satışı daha önce gerçekleşmişse (ör. eski kayıtların girilmesi),
            // "4. Satış Bilgileri" bölümünde doldurulan satırlar da Satış kaydı olarak eklenir.
            var genelAyar = await _db.GenelAyarlar.FirstOrDefaultAsync();
            var gunlukOtoparkUcreti = genelAyar?.GunlukOtoparkUcreti ?? 0m;

            if (satis1Tarihi.HasValue && satis1Sonuc.HasValue)
            {
                dosya.Satislar.Add(new Satis
                {
                    SatisTuru = SatisTuru.BirinciSatis,
                    SatisTarihi = satis1Tarihi.Value,
                    SatisSonucu = satis1Sonuc.Value,
                    SatisBedeli = satis1Sonuc == SatisSonucu.Satildi ? satis1Bedel : null,
                    AliciAdiSoyadi = satis1Alici,
                    OtoparkaGirisTarihi = dosya.OtoparkaGirisTarihi,
                    GunlukOtoparkUcreti = gunlukOtoparkUcreti
                });
            }
            if (satis2Tarihi.HasValue && satis2Sonuc.HasValue)
            {
                dosya.Satislar.Add(new Satis
                {
                    SatisTuru = SatisTuru.IkinciSatis,
                    SatisTarihi = satis2Tarihi.Value,
                    SatisSonucu = satis2Sonuc.Value,
                    SatisBedeli = satis2Sonuc == SatisSonucu.Satildi ? satis2Bedel : null,
                    AliciAdiSoyadi = satis2Alici,
                    OtoparkaGirisTarihi = dosya.OtoparkaGirisTarihi,
                    GunlukOtoparkUcreti = gunlukOtoparkUcreti
                });
            }

            if (dosya.Satislar.Any(s => s.SatisSonucu == SatisSonucu.Satildi))
            {
                dosya.Durum = DosyaDurumu.Satildi;
            }
            else if (dosya.Satislar.Any())
            {
                dosya.Durum = DosyaDurumu.SatisaCikti;
            }

            _db.Dosyalar.Add(dosya);
            await _db.SaveChangesAsync();

            TempData["Basari"] = $"Dosya başarıyla oluşturuldu. Dosya No: {dosya.DosyaNo}";
            return RedirectToAction(nameof(Details), new { id = dosya.Id });
        }

        // GET: /Dosya/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var dosya = await _db.Dosyalar.FindAsync(id);
            if (dosya == null) return NotFound();
            await DoldurListeler();
            return View(dosya);
        }

        // POST: /Dosya/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Dosya guncel)
        {
            if (id != guncel.Id) return NotFound();

            var dosya = await _db.Dosyalar.FindAsync(id);
            if (dosya == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await DoldurListeler();
                return View(guncel);
            }

            // Dosya no ve kayıt tarihi değiştirilemez; diğer alanlar güncellenir.
            dosya.VergiDairesiAdi = guncel.VergiDairesiAdi;
            dosya.VergiDairesiYaziTarihi = guncel.VergiDairesiYaziTarihi;
            dosya.VergiDairesiYaziSayisi = guncel.VergiDairesiYaziSayisi;
            dosya.AlacakliVergiDairesi = guncel.AlacakliVergiDairesi;
            dosya.VergiKimlikNumarasi = guncel.VergiKimlikNumarasi;
            dosya.TCKimlikNumarasi = guncel.TCKimlikNumarasi;
            dosya.AdiSoyadiUnvani = guncel.AdiSoyadiUnvani;
            dosya.KanuniTemsilci = guncel.KanuniTemsilci;
            dosya.Plaka = guncel.Plaka;
            dosya.AracCinsi = guncel.AracCinsi;
            dosya.AracMarkasi = guncel.AracMarkasi;
            dosya.AracTipi = guncel.AracTipi;
            dosya.ModelYili = guncel.ModelYili;
            dosya.Renk = guncel.Renk;
            dosya.RuhsatVar = guncel.RuhsatVar;
            dosya.AnahtarVar = guncel.AnahtarVar;
            dosya.CekiciVar = guncel.CekiciVar;
            dosya.OtoparkAdi = guncel.OtoparkAdi;
            dosya.HasarDurumu = guncel.HasarDurumu;
            dosya.MuhammenBedel = guncel.MuhammenBedel;
            dosya.KdvOrani = guncel.KdvOrani;
            dosya.OtoparkaGirisTarihi = guncel.OtoparkaGirisTarihi;
            dosya.KomisyonBaskani = guncel.KomisyonBaskani;
            dosya.KomisyonUye1 = guncel.KomisyonUye1;
            dosya.KomisyonUye2 = guncel.KomisyonUye2;
            dosya.KomisyonUye3 = guncel.KomisyonUye3;
            dosya.KomisyonUye4 = guncel.KomisyonUye4;

            await _db.SaveChangesAsync();
            TempData["Basari"] = "Dosya güncellendi.";
            return RedirectToAction(nameof(Details), new { id = dosya.Id });
        }

        // 2. Satış İhale Tutanağı - yazdırılabilir resmi belge.
        public async Task<IActionResult> IhaleTutanagi(int id)
        {
            var dosya = await _db.Dosyalar
                .Include(d => d.Satislar)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (dosya == null) return NotFound();
            return View(dosya);
        }

        // Tek bir dosyanın tüm bilgilerini Excel olarak indirir.
        public async Task<IActionResult> ExcelIndir(int id)
        {
            var dosya = await _db.Dosyalar
                .Include(d => d.Satislar)
                .Include(d => d.IptalIade)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (dosya == null) return NotFound();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Dosya Bilgileri");
            var satir = 1;
            void Yaz(string alan, object? deger)
            {
                ws.Cell(satir, 1).Value = alan;
                ws.Cell(satir, 1).Style.Font.Bold = true;
                ws.Cell(satir, 2).Value = deger?.ToString() ?? "";
                satir++;
            }
            Yaz("Dosya No", dosya.DosyaNo);
            Yaz("Kayıt Tarihi", dosya.KayitTarihi.ToString("dd.MM.yyyy HH:mm"));
            Yaz("Vergi Dairesi", dosya.VergiDairesiAdi);
            Yaz("Alacaklı Vergi Dairesi", dosya.AlacakliVergiDairesi);
            Yaz("TCKN", dosya.TCKimlikNumarasi);
            Yaz("VKN", dosya.VergiKimlikNumarasi);
            Yaz("Adı Soyadı / Unvanı", dosya.AdiSoyadiUnvani);
            Yaz("Plaka", dosya.Plaka);
            Yaz("Marka", dosya.AracMarkasi);
            Yaz("Cins", dosya.AracCinsi);
            Yaz("Model Yılı", dosya.ModelYili);
            Yaz("Muhammen Bedel (TL)", dosya.MuhammenBedel);
            Yaz("Teminat (TL)", dosya.Teminat);
            Yaz("Damga Vergisi (TL)", dosya.DamgaVergisiTutari);
            Yaz("Durum", dosya.Durum);
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Dosya_{dosya.DosyaNo.Replace("/", "_")}.xlsx");
        }

        private async Task DoldurListeler()
        {
            ViewBag.Cinsler = await _db.AracCinsleri.OrderBy(a => a.Ad).ToListAsync();
            ViewBag.Markalar = await _db.AracMarkalari.OrderBy(a => a.Ad).ToListAsync();
            ViewBag.Otoparklar = await _db.Otoparklar.OrderBy(a => a.Ad).ToListAsync();
            ViewBag.Renkler = await _db.AracRenkleri.OrderBy(a => a.Ad).ToListAsync();

            var vergiDaireleri = await _db.VergiDaireleri.OrderBy(a => a.Sehir).ThenBy(a => a.Ad).ToListAsync();

            // "İlgili Vergi Dairesi" alanı - sadece Ankara'daki vergi daireleri (il seçimine gerek yok).
            ViewBag.AnkaraVergiDaireleri = vergiDaireleri.Where(v => v.Sehir == "Ankara").OrderBy(v => v.Ad).ToList();

            // "Alacaklı Vergi Dairesi" alanı - Türkiye geneli, il seçimine göre kademeli (cascading).
            ViewBag.Sehirler = vergiDaireleri.Select(v => v.Sehir).Distinct().OrderBy(s => s).ToList();
            ViewBag.VergiDaireleriJson = System.Text.Json.JsonSerializer.Serialize(
                vergiDaireleri.Select(v => new { v.Id, v.Ad, v.Sehir }));
        }
    }
}
