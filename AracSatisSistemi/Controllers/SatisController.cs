using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Controllers
{
    public class SatisController : Controller
    {
        private readonly AppDbContext _db;
        public SatisController(AppDbContext db) => _db = db;

        // Dosya no ile arama ekranı (SATIŞA İLİŞKİN BİLGİLER giriş noktası)
        public IActionResult Index(string? dosyaNo)
        {
            ViewBag.DosyaNo = dosyaNo;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Bul(string dosyaNo)
        {
            var dosya = await _db.Dosyalar.FirstOrDefaultAsync(d => d.DosyaNo == dosyaNo);
            if (dosya == null)
            {
                TempData["Hata"] = "Bu dosya numarasına ait kayıt bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Create), new { dosyaId = dosya.Id });
        }

        public async Task<IActionResult> Create(int dosyaId)
        {
            var dosya = await _db.Dosyalar.FindAsync(dosyaId);
            if (dosya == null) return NotFound();

            var genelAyar = await _db.GenelAyarlar.FirstOrDefaultAsync();

            var satis = new Satis
            {
                DosyaId = dosyaId,
                SatisTarihi = DateTime.Today,
                OtoparkaGirisTarihi = dosya.OtoparkaGirisTarihi,
                GunlukOtoparkUcreti = genelAyar?.GunlukOtoparkUcreti ?? 0m
            };
            ViewBag.Dosya = dosya;
            return View(satis);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Satis satis)
        {
            var dosya = await _db.Dosyalar.FindAsync(satis.DosyaId);
            if (dosya == null) return NotFound();

            // Satış sonucu "Alıcı Çıkmadı" ise satış bedeli sıfırlanır (pasif alan).
            if (satis.SatisSonucu == SatisSonucu.AliciCikmadi)
            {
                satis.SatisBedeli = null;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Dosya = dosya;
                return View(satis);
            }

            _db.Satislar.Add(satis);

            dosya.Durum = satis.SatisSonucu == SatisSonucu.Satildi
                ? DosyaDurumu.Satildi
                : DosyaDurumu.SatisaCikti;

            await _db.SaveChangesAsync();
            TempData["Basari"] = "Satış bilgileri kaydedildi.";
            return RedirectToAction("Details", "Dosya", new { id = dosya.Id });
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
