using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;
using ClosedXML.Excel;

namespace AracSatisSistemi.Controllers
{
    public class SatisController : Controller
    {
        private readonly AppDbContext _db;
        public SatisController(AppDbContext db) => _db = db;

        // Bir türün "bir önceki" türünü verir (sıra: 1. Satış -> 2. Satış -> Pazarlık -> 6183/86).
        private static SatisTuru? OncekiTur(SatisTuru tur) => tur switch
        {
            SatisTuru.IkinciSatis => SatisTuru.BirinciSatis,
            SatisTuru.Pazarlik => SatisTuru.IkinciSatis,
            SatisTuru.Madde6183_86 => SatisTuru.Pazarlik,
            _ => null
        };

        // Belleğe alınmış (Satislar dahil) bir dosyanın, verilen tür için şu an sonuç
        // girilmeye uygun olup olmadığını kontrol eder: bu tür için henüz sonuç girilmemiş
        // olmalı ve (1. Satış hariç) bir önceki türün sonucu "Alıcı Çıkmadı" olmalı.
        private static bool TurGecerliMi(Dosya dosya, SatisTuru tur)
        {
            if (dosya.Satislar.Any(s => s.SatisTuru == tur)) return false;
            var onceki = OncekiTur(tur);
            if (onceki == null) return true;
            var oncekiKayit = dosya.Satislar.FirstOrDefault(s => s.SatisTuru == onceki);
            return oncekiKayit != null && oncekiKayit.SatisSonucu == SatisSonucu.AliciCikmadi;
        }

        // Bir dosya için şu an sonuç girilmeye uygun olan (varsa) türü ve önerilen tarihi bulur.
        private static (SatisTuru tur, DateTime tarih)? SonrakiAdim(Dosya dosya)
        {
            foreach (var tur in new[] { SatisTuru.BirinciSatis, SatisTuru.IkinciSatis, SatisTuru.Pazarlik, SatisTuru.Madde6183_86 })
            {
                if (!TurGecerliMi(dosya, tur)) continue;
                var tarih = tur switch
                {
                    SatisTuru.BirinciSatis => dosya.Satis1Tarihi,
                    SatisTuru.IkinciSatis => dosya.Satis2Tarihi,
                    _ => DateTime.Today
                };
                return (tur, tarih);
            }
            return null;
        }

        // GET: /Satis - Adım 1: Satış Türü ve Satış Tarihi seçimi.
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Satis/Listele?tur=..&tarih=.. - Adım 2: seçilen tür/tarihte ihaleye giren araçlar.
        public async Task<IActionResult> Listele(SatisTuru tur, DateTime tarih)
        {
            var tarihGunu = tarih.Date;
            IQueryable<Dosya> sorgu = _db.Dosyalar.Include(d => d.Satislar);

            sorgu = tur switch
            {
                SatisTuru.BirinciSatis => sorgu
                    .Where(d => d.Satis1Tarihi.Date == tarihGunu)
                    .Where(d => !d.Satislar.Any(s => s.SatisTuru == SatisTuru.BirinciSatis)),

                SatisTuru.IkinciSatis => sorgu
                    .Where(d => d.Satis2Tarihi.Date == tarihGunu)
                    .Where(d => d.Satislar.Any(s => s.SatisTuru == SatisTuru.BirinciSatis && s.SatisSonucu == SatisSonucu.AliciCikmadi))
                    .Where(d => !d.Satislar.Any(s => s.SatisTuru == SatisTuru.IkinciSatis)),

                SatisTuru.Pazarlik => sorgu
                    .Where(d => d.Satislar.Any(s => s.SatisTuru == SatisTuru.IkinciSatis && s.SatisSonucu == SatisSonucu.AliciCikmadi))
                    .Where(d => !d.Satislar.Any(s => s.SatisTuru == SatisTuru.Pazarlik)),

                SatisTuru.Madde6183_86 => sorgu
                    .Where(d => d.Satislar.Any(s => s.SatisTuru == SatisTuru.Pazarlik && s.SatisSonucu == SatisSonucu.AliciCikmadi))
                    .Where(d => !d.Satislar.Any(s => s.SatisTuru == SatisTuru.Madde6183_86)),

                _ => sorgu.Where(d => false)
            };

            var bekleyenler = await sorgu.OrderBy(d => d.DosyaNo).ToListAsync();

            // Bu tür/tarih için daha önce (bu oturumda veya önceden) girilmiş sonuçlar - geri bildirim amaçlı.
            var sonuclananlar = await _db.Satislar
                .Include(s => s.Dosya)
                .Where(s => s.SatisTuru == tur && s.SatisTarihi.Date == tarihGunu)
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            ViewBag.Tur = tur;
            ViewBag.Tarih = tarihGunu;
            ViewBag.Sonuclananlar = sonuclananlar;
            return View(bekleyenler);
        }

        // Türün resmi belgelerde kullanılan adı (ör. "1. SATIŞ").
        private static string TurAdiBuyuk(SatisTuru tur) => tur switch
        {
            SatisTuru.BirinciSatis => "1. SATIŞ",
            SatisTuru.IkinciSatis => "2. SATIŞ",
            SatisTuru.Pazarlik => "PAZARLIK",
            SatisTuru.Madde6183_86 => "6183/86 MAD.",
            _ => tur.ToString()
        };

        private async Task<List<Satis>> SonucListesiGetir(SatisTuru tur, DateTime tarih)
        {
            var tarihGunu = tarih.Date;
            return await _db.Satislar
                .Include(s => s.Dosya)
                .Where(s => s.SatisTuru == tur && s.SatisTarihi.Date == tarihGunu)
                .OrderBy(s => s.Dosya!.DosyaNo)
                .ToListAsync();
        }

        // GET: /Satis/Liste?tur=..&tarih=.. - Resmi "Satış Listesi" yazdırma/PDF çıktısı.
        public async Task<IActionResult> Liste(SatisTuru tur, DateTime tarih)
        {
            var liste = await SonucListesiGetir(tur, tarih);
            ViewBag.Tur = tur;
            ViewBag.Tarih = tarih.Date;
            ViewBag.TurAdiBuyuk = TurAdiBuyuk(tur);
            return View(liste);
        }

        // "Satış Listesi" çıktısını Excel olarak indirir.
        public async Task<IActionResult> ListeExcelIndir(SatisTuru tur, DateTime tarih)
        {
            var liste = await SonucListesiGetir(tur, tarih);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Satis Listesi");
            ws.Cell(1, 1).Value = "ANKARA DEFTERDARLIĞI";
            ws.Cell(2, 1).Value = $"{TurAdiBuyuk(tur)} LİSTESİ  {tarih.Date:dd.MM.yyyy}";

            string[] baslik = { "Sıra No", "Dosya No", "Vergi Dairesi", "Plaka", "Model", "Markası/Cinsi", "KDV", "Muammen Bedel", "(%) 75", "(%) 40", "Teminat", "Satış Sonucu", "Satış Bedeli", "SB/MB %" };
            var basSatir = 4;
            for (int i = 0; i < baslik.Length; i++) ws.Cell(basSatir, i + 1).Value = baslik[i];

            var satir = basSatir + 1;
            var sira = 1;
            decimal muhammenToplam = 0m;
            decimal satisBedeliToplam = 0m;
            foreach (var s in liste)
            {
                var d = s.Dosya!;
                muhammenToplam += d.MuhammenBedel;
                satisBedeliToplam += s.SatisBedeli ?? 0m;
                ws.Cell(satir, 1).Value = sira++;
                ws.Cell(satir, 2).Value = d.DosyaNo;
                ws.Cell(satir, 3).Value = d.VergiDairesiAdi;
                ws.Cell(satir, 4).Value = d.Plaka;
                ws.Cell(satir, 5).Value = d.ModelYili;
                ws.Cell(satir, 6).Value = $"{d.AracMarkasi} / {d.AracTipi} ({d.AracCinsi})";
                ws.Cell(satir, 7).Value = $"%{d.KdvOrani}";
                ws.Cell(satir, 8).Value = d.MuhammenBedel;
                ws.Cell(satir, 9).Value = d.MuhammenBedel * 0.75m;
                ws.Cell(satir, 10).Value = d.MuhammenBedel * 0.40m;
                ws.Cell(satir, 11).Value = d.Teminat;
                ws.Cell(satir, 12).Value = $"{s.SatisTuruGorunen} {s.SonucGorunen}";
                ws.Cell(satir, 13).Value = s.SatisBedeli;
                ws.Cell(satir, 14).Value = s.SatisBedeli.HasValue && d.MuhammenBedel != 0
                    ? $"%{(s.SatisBedeli.Value / d.MuhammenBedel * 100m):N2}"
                    : "";
                satir++;
            }

            if (liste.Count > 0)
            {
                ws.Cell(satir, 7).Value = "TOPLAM";
                ws.Cell(satir, 7).Style.Font.Bold = true;
                ws.Cell(satir, 8).Value = muhammenToplam;
                ws.Cell(satir, 8).Style.Font.Bold = true;
                ws.Cell(satir, 13).Value = satisBedeliToplam;
                ws.Cell(satir, 13).Style.Font.Bold = true;
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            var dosyaAdi = $"SatisListesi_{TurAdiBuyuk(tur).Replace("/", "-").Replace(" ", "")}_{tarih.Date:yyyyMMdd}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", dosyaAdi);
        }

        // GET: /Satis/SonucGir?dosyaId=..&tur=..&tarih=.. - Adım 3: seçilen aracın sonucunu girme formu.
        public async Task<IActionResult> SonucGir(int dosyaId, SatisTuru tur, DateTime tarih)
        {
            var dosya = await _db.Dosyalar.Include(d => d.Satislar).FirstOrDefaultAsync(d => d.Id == dosyaId);
            if (dosya == null) return NotFound();

            if (!TurGecerliMi(dosya, tur))
            {
                TempData["Hata"] = "Bu dosya için bu satış türünde sonuç girişi şu an uygun değil (sonuç zaten girilmiş olabilir veya bir önceki aşama tamamlanmamış).";
                return RedirectToAction(nameof(Listele), new { tur, tarih });
            }

            var genelAyar = await _db.GenelAyarlar.FirstOrDefaultAsync();
            var satis = new Satis
            {
                DosyaId = dosyaId,
                SatisTuru = tur,
                SatisTarihi = tarih.Date,
                OtoparkaGirisTarihi = dosya.OtoparkaGirisTarihi,
                GunlukOtoparkUcreti = genelAyar?.GunlukOtoparkUcreti ?? 0m
            };
            ViewBag.Dosya = dosya;
            return View(satis);
        }

        // POST: /Satis/SonucGir
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SonucGir(Satis satis)
        {
            var dosya = await _db.Dosyalar.Include(d => d.Satislar).FirstOrDefaultAsync(d => d.Id == satis.DosyaId);
            if (dosya == null) return NotFound();

            if (satis.SatisSonucu == SatisSonucu.AliciCikmadi)
            {
                satis.SatisBedeli = null;
            }

            if (!TurGecerliMi(dosya, satis.SatisTuru))
            {
                TempData["Hata"] = "Bu dosya için bu satış türünde sonuç girişi şu an uygun değil (sonuç zaten girilmiş olabilir veya bir önceki aşama tamamlanmamış).";
                return RedirectToAction(nameof(Listele), new { tur = satis.SatisTuru, tarih = satis.SatisTarihi });
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Dosya = dosya;
                return View(satis);
            }

            _db.Satislar.Add(satis);

            // Araç satıldıysa dosya PASİF'e düşer (Durum = Satıldı); alıcı çıkmadıysa dosya
            // AKTİF kalır (Durum = Satışta) ki bir sonraki aşamaya (2. Satış / Pazarlık / 6183-86) geçilebilsin.
            dosya.Durum = satis.SatisSonucu == SatisSonucu.Satildi
                ? DosyaDurumu.Satildi
                : DosyaDurumu.SatisaCikti;

            await _db.SaveChangesAsync();
            TempData["Basari"] = $"{dosya.DosyaNo} nolu dosya için satış sonucu kaydedildi.";
            return RedirectToAction(nameof(Listele), new { tur = satis.SatisTuru, tarih = satis.SatisTarihi });
        }

        // Dosya Detayı ekranındaki "Satış Bilgisi Gir" butonu buraya yönlenir; dosyanın şu anki
        // aşamasını otomatik tespit edip doğrudan SonucGir formuna yönlendirir.
        public async Task<IActionResult> DosyaIcinYonlendir(int dosyaId)
        {
            var dosya = await _db.Dosyalar.Include(d => d.Satislar).FirstOrDefaultAsync(d => d.Id == dosyaId);
            if (dosya == null) return NotFound();

            var adim = SonrakiAdim(dosya);
            if (adim == null)
            {
                TempData["Hata"] = "Bu dosya için girilecek yeni bir satış sonucu bulunmuyor.";
                return RedirectToAction("Details", "Dosya", new { id = dosyaId });
            }
            return RedirectToAction(nameof(SonucGir), new { dosyaId, tur = adim.Value.tur, tarih = adim.Value.tarih });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var satis = await _db.Satislar.Include(s => s.Dosya).FirstOrDefaultAsync(s => s.Id == id);
            if (satis == null) return NotFound();
            ViewBag.Dosya = satis.Dosya;
            return View(satis);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Satis guncel)
        {
            var satis = await _db.Satislar.Include(s => s.Dosya).FirstOrDefaultAsync(s => s.Id == id);
            if (satis == null) return NotFound();

            if (guncel.SatisSonucu == SatisSonucu.AliciCikmadi)
            {
                guncel.SatisBedeli = null;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Dosya = satis.Dosya;
                return View(guncel);
            }

            satis.SatisTuru = guncel.SatisTuru;
            satis.SatisTarihi = guncel.SatisTarihi;
            satis.SatisSonucu = guncel.SatisSonucu;
            satis.SatisBedeli = guncel.SatisBedeli;
            satis.AliciTCKN = guncel.AliciTCKN;
            satis.AliciVKN = guncel.AliciVKN;
            satis.AliciAdiSoyadi = guncel.AliciAdiSoyadi;
            satis.Adres = guncel.Adres;
            satis.MTV = guncel.MTV;
            satis.OtoparkaGirisTarihi = guncel.OtoparkaGirisTarihi;
            satis.GunlukOtoparkUcreti = guncel.GunlukOtoparkUcreti;

            if (satis.Dosya != null)
            {
                satis.Dosya.Durum = satis.SatisSonucu == SatisSonucu.Satildi
                    ? DosyaDurumu.Satildi
                    : DosyaDurumu.SatisaCikti;
            }

            await _db.SaveChangesAsync();
            TempData["Basari"] = "Satış bilgileri güncellendi.";
            return RedirectToAction("Details", "Dosya", new { id = satis.DosyaId });
        }
    }
}
