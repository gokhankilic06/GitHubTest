using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;
using ClosedXML.Excel;

namespace AracSatisSistemi.Controllers
{
    // YILLIK SATIŞ TAKVİMİ: idarenin bir yıl boyunca uygulayacağı 1. ve 2. Satış
    // (ihale) günlerini önceden belirleyip vergi dairelerine bildirdiği resmi takvim.
    // Yeni bir dosya kaydedilirken 1./2. Satış tarihleri buradaki listeden (bugünden
    // sonraki ilk iki tarih) otomatik önerilir; Komisyon İşlemleri ekranındaki Satış
    // Tarihi seçimi de bu listeden gelir.
    public class SatisTakvimiController : Controller
    {
        private readonly AppDbContext _db;
        public SatisTakvimiController(AppDbContext db) => _db = db;

        // Her takvim tarihi için o tarihte 1./2. Satış'ını BEKLEYEN (henüz sonucu
        // girilmemiş) dosya sayısını hesaplar; takvimi "Satış Sonuçlarını Gir" akışına
        // doğrudan bağlayan sayaç + bağlantı burada üretilir (bkz. Index.cshtml).
        private async Task<(Dictionary<DateTime, int> Birinci, Dictionary<DateTime, int> Ikinci)> PlanliDosyaSayilariGetir()
        {
            var aktifDosyalar = await _db.Dosyalar.Include(d => d.Satislar)
                .Where(d => d.Durum == DosyaDurumu.Kayitli || d.Durum == DosyaDurumu.SatisaCikti)
                .ToListAsync();

            var birinci = aktifDosyalar.Where(d => d.Asama == Dosya.SurecAsamasi.BirinciSatisBekliyor)
                .GroupBy(d => d.Satis1Tarihi.Date)
                .ToDictionary(g => g.Key, g => g.Count());
            var ikinci = aktifDosyalar.Where(d => d.Asama == Dosya.SurecAsamasi.IkinciSatisBekliyor)
                .GroupBy(d => d.Satis2Tarihi.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            return (birinci, ikinci);
        }

        public async Task<IActionResult> Index()
        {
            var tumTarihler = await _db.SatisTarihleri.OrderBy(t => t.Tarih).ToListAsync();
            var resmiTatiller = await _db.ResmiTatiller.Select(t => t.Tarih.Date).ToListAsync();
            ViewBag.ResmiTatiller = resmiTatiller;
            ViewBag.Bugun = DateTime.Today;

            var (birinci, ikinci) = await PlanliDosyaSayilariGetir();
            ViewBag.BirinciSatisSayilari = birinci;
            ViewBag.IkinciSatisSayilari = ikinci;

            return View(tumTarihler);
        }

        // Takvimi, her tarihteki planlı dosya sayılarıyla birlikte Excel olarak indirir.
        public async Task<IActionResult> ExcelIndir()
        {
            var tumTarihler = await _db.SatisTarihleri.OrderBy(t => t.Tarih).ToListAsync();
            var resmiTatiller = (await _db.ResmiTatiller.Select(t => t.Tarih.Date).ToListAsync()).ToHashSet();
            var bugun = DateTime.Today;
            var trTR = new System.Globalization.CultureInfo("tr-TR");
            var (birinci, ikinci) = await PlanliDosyaSayilariGetir();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Satış Takvimi");
            string[] baslik = { "Tarih", "Gün", "Durum", "Hafta Sonu mu", "Resmi Tatil mi", "1. Satış İçin Planlı Dosya", "2. Satış İçin Planlı Dosya" };
            for (int i = 0; i < baslik.Length; i++) ws.Cell(1, i + 1).Value = baslik[i];

            var satir = 2;
            foreach (var t in tumTarihler)
            {
                var gun = t.Tarih.Date;
                ws.Cell(satir, 1).Value = gun.ToString("dd.MM.yyyy");
                ws.Cell(satir, 2).Value = gun.ToString("dddd", trTR);
                ws.Cell(satir, 3).Value = gun >= bugun ? "Gelecek" : "Geçmiş";
                ws.Cell(satir, 4).Value = (gun.DayOfWeek == DayOfWeek.Saturday || gun.DayOfWeek == DayOfWeek.Sunday) ? "Evet" : "Hayır";
                ws.Cell(satir, 5).Value = resmiTatiller.Contains(gun) ? "Evet" : "Hayır";
                ws.Cell(satir, 6).Value = birinci.TryGetValue(gun, out var b) ? b : 0;
                ws.Cell(satir, 7).Value = ikinci.TryGetValue(gun, out var i2) ? i2 : 0;
                satir++;
            }
            ws.Columns().AdjustToContents();
            ws.Row(1).Style.Font.Bold = true;

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SatisTakvimi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        // POST: /SatisTakvimi/Ekle - tek bir tarih ekler.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(DateTime tarih)
        {
            var gun = tarih.Date;
            if (!await _db.SatisTarihleri.AnyAsync(t => t.Tarih == gun))
            {
                _db.SatisTarihleri.Add(new SatisTarihi { Tarih = gun });
                await _db.SaveChangesAsync();
                TempData["Basari"] = $"{gun:dd.MM.yyyy} satış takvimine eklendi.";
            }
            else
            {
                TempData["Hata"] = $"{gun:dd.MM.yyyy} zaten takvimde kayıtlı.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /SatisTakvimi/TopluEkle - başlangıç/bitiş arasındaki tüm Çarşambaları
        // (resmi tatiller hariç) hızlıca ekler; zaten takvimde olanlar atlanır.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> TopluEkle(DateTime baslangic, DateTime bitis)
        {
            if (bitis < baslangic)
            {
                TempData["Hata"] = "Bitiş tarihi başlangıçtan önce olamaz.";
                return RedirectToAction(nameof(Index));
            }

            var resmiTatiller = await _db.ResmiTatiller.Select(t => t.Tarih.Date).ToListAsync();
            var adaylar = IhaleTarihHesaplayici.YilinSatisGunleri(baslangic, bitis, resmiTatiller);
            var mevcutlar = (await _db.SatisTarihleri
                    .Where(t => t.Tarih >= baslangic.Date && t.Tarih <= bitis.Date)
                    .Select(t => t.Tarih)
                    .ToListAsync())
                .ToHashSet();

            var eklenecekler = adaylar.Where(t => !mevcutlar.Contains(t)).Select(t => new SatisTarihi { Tarih = t }).ToList();
            if (eklenecekler.Count > 0)
            {
                _db.SatisTarihleri.AddRange(eklenecekler);
                await _db.SaveChangesAsync();
            }
            TempData["Basari"] = $"{eklenecekler.Count} yeni tarih (haftalık Çarşamba, resmi tatiller hariç) takvime eklendi.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /SatisTakvimi/ExcelYukle - idarenin hazırladığı resmi ihale programı Excel
        // dosyasından (hangi sütunda/biçimde olduğuna bakılmaksızın) tüm geçerli tarihleri
        // okuyup takvime ekler; zaten kayıtlı olanlar atlanır. Güncellemeler ekranındaki
        // "Satış Takvimi" bölümünden kullanılır.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcelYukle(IFormFile dosya)
        {
            if (dosya == null || dosya.Length == 0)
            {
                TempData["Hata"] = "Lütfen bir Excel (.xlsx) dosyası seçin.";
                return RedirectToAction("Index", "Ayarlar");
            }

            var bulunanTarihler = new HashSet<DateTime>();
            var trTR = new System.Globalization.CultureInfo("tr-TR");
            try
            {
                using var stream = dosya.OpenReadStream();
                using var wb = new XLWorkbook(stream);
                foreach (var ws in wb.Worksheets)
                {
                    var kullanilanAlan = ws.RangeUsed();
                    if (kullanilanAlan == null) continue;
                    foreach (var hucre in kullanilanAlan.CellsUsed())
                    {
                        DateTime? tarih = null;
                        if (hucre.DataType == XLDataType.DateTime)
                        {
                            tarih = hucre.GetDateTime();
                        }
                        else
                        {
                            var metin = hucre.GetString().Trim();
                            // Yalnızca tarih ayırıcısı içeren metinler denenir; aksi halde
                            // sayısal/metin başlıkları ("1. Satış" gibi) yanlışlıkla tarih
                            // olarak algılanabilir.
                            if (metin.IndexOfAny(new[] { '.', '/', '-' }) >= 0 &&
                                (DateTime.TryParse(metin, trTR, System.Globalization.DateTimeStyles.None, out var ayristirilan)
                                 || DateTime.TryParse(metin, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out ayristirilan)))
                            {
                                tarih = ayristirilan;
                            }
                        }
                        if (tarih.HasValue && tarih.Value.Year >= 2000 && tarih.Value.Year <= 2100)
                        {
                            bulunanTarihler.Add(tarih.Value.Date);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Hata"] = "Excel dosyası okunamadı: " + ex.Message;
                return RedirectToAction("Index", "Ayarlar");
            }

            if (bulunanTarihler.Count == 0)
            {
                TempData["Hata"] = "Excel dosyasında okunabilir bir tarih bulunamadı.";
                return RedirectToAction("Index", "Ayarlar");
            }

            var mevcutlar = (await _db.SatisTarihleri.Select(t => t.Tarih).ToListAsync()).ToHashSet();
            var eklenecekler = bulunanTarihler.Where(t => !mevcutlar.Contains(t)).Select(t => new SatisTarihi { Tarih = t }).ToList();
            if (eklenecekler.Count > 0)
            {
                _db.SatisTarihleri.AddRange(eklenecekler);
                await _db.SaveChangesAsync();
            }

            var zatenKayitli = bulunanTarihler.Count - eklenecekler.Count;
            TempData["Basari"] = $"Excel'den {bulunanTarihler.Count} tarih okundu; {eklenecekler.Count} yeni tarih satış takvimine eklendi" +
                (zatenKayitli > 0 ? $" ({zatenKayitli} tanesi zaten kayıtlıydı)." : ".");
            return RedirectToAction("Index", "Ayarlar");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            var kayit = await _db.SatisTarihleri.FindAsync(id);
            if (kayit != null)
            {
                _db.SatisTarihleri.Remove(kayit);
                await _db.SaveChangesAsync();
                TempData["Basari"] = $"{kayit.Tarih:dd.MM.yyyy} takvimden silindi.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
