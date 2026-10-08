using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;

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

        public async Task<IActionResult> Index()
        {
            var tumTarihler = await _db.SatisTarihleri.OrderBy(t => t.Tarih).ToListAsync();
            var resmiTatiller = await _db.ResmiTatiller.Select(t => t.Tarih.Date).ToListAsync();
            ViewBag.ResmiTatiller = resmiTatiller;
            ViewBag.Bugun = DateTime.Today;
            return View(tumTarihler);
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
