using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Controllers
{
    // KOMİSYON ÜYE GİRİŞ İŞLEMLERİ ekranı: belirli bir satış türü/tarihi/saatinde
    // ihaleyi yürütecek komisyonun (başkan + 3 üye) kaydedilmesi.
    public class KomisyonController : Controller
    {
        private readonly AppDbContext _db;
        public KomisyonController(AppDbContext db) => _db = db;

        private async Task DoldurListeler()
        {
            ViewBag.Memurlar = await _db.Memurlar.OrderBy(m => m.AdSoyad).ToListAsync();

            var resmiTatiller = await _db.ResmiTatiller.Select(t => t.Tarih.Date).ToListAsync();
            var bugun = DateTime.Today;
            ViewBag.SatisGunleri = IhaleTarihHesaplayici.YilinSatisGunleri(bugun, bugun.AddYears(1), resmiTatiller);
        }

        // GET: /Komisyon - liste + yeni kayıt formu
        public async Task<IActionResult> Index()
        {
            await DoldurListeler();
            // SQLite, ORDER BY içinde TimeSpan tipini desteklemediğinden ikincil sıralama
            // (saat) istemci tarafında yapılır.
            var liste = (await _db.KomisyonOturumlari.ToListAsync())
                .OrderByDescending(k => k.SatisTarihi)
                .ThenByDescending(k => k.SatisSaati)
                .ToList();
            return View(liste);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KomisyonOturumu form)
        {
            if (!ModelState.IsValid)
            {
                TempData["Hata"] = "Lütfen tüm zorunlu alanları doldurun (Satış Türü, Tarihi, Saati, Başkan ve 3 Üye).";
                return RedirectToAction(nameof(Index));
            }

            _db.KomisyonOturumlari.Add(form);
            await _db.SaveChangesAsync();
            TempData["Basari"] = $"{form.Id} nolu komisyon oturumu kaydedildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            var kayit = await _db.KomisyonOturumlari.FindAsync(id);
            if (kayit != null)
            {
                _db.KomisyonOturumlari.Remove(kayit);
                await _db.SaveChangesAsync();
                TempData["Basari"] = $"{id} nolu komisyon oturumu silindi.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
