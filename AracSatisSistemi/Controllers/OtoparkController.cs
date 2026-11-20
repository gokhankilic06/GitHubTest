using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Controllers
{
    // OTOPARK LİSTESİ ekranı: yediemin otoparklarının adı, adresi, yetkili/çalışanı ve
    // telefonunun eklenip/düzenlenip/silinebildiği kendi sayfası. "Ad" alanı Kayıt
    // İşlemleri ekranındaki "Bulunduğu Otopark" listesinde kullanılır.
    public class OtoparkController : Controller
    {
        private readonly AppDbContext _db;
        public OtoparkController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var otoparklar = await _db.Otoparklar.OrderBy(o => o.Ad).ToListAsync();
            return View(otoparklar);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(OtoparkKaydi form)
        {
            if (string.IsNullOrWhiteSpace(form.Ad))
            {
                TempData["Hata"] = "Otopark adı zorunludur.";
                return RedirectToAction(nameof(Index));
            }

            _db.Otoparklar.Add(new OtoparkKaydi
            {
                Ad = form.Ad.Trim(),
                Adres = form.Adres?.Trim(),
                YetkiliCalisani = form.YetkiliCalisani?.Trim(),
                Telefon = form.Telefon?.Trim()
            });
            await _db.SaveChangesAsync();
            TempData["Basari"] = $"\"{form.Ad.Trim()}\" eklendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Duzenle(OtoparkKaydi form)
        {
            var kayit = await _db.Otoparklar.FindAsync(form.Id);
            if (kayit == null || string.IsNullOrWhiteSpace(form.Ad))
            {
                TempData["Hata"] = "Otopark güncellenemedi.";
                return RedirectToAction(nameof(Index));
            }

            // Bu otoparkın eski adı Dosya/Satış kayıtlarında kullanılmış olabilir; isim
            // değişirse o kayıtlardaki metin de güncellenir, aksi halde dropdown'daki yeni
            // adla eski kayıtlar arasındaki bağ kopar.
            var eskiAd = kayit.Ad;
            var yeniAd = form.Ad.Trim();
            if (eskiAd != yeniAd)
            {
                await _db.Dosyalar.Where(d => d.OtoparkAdi == eskiAd)
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.OtoparkAdi, yeniAd));
                await _db.Satislar.Where(s => s.FaturayiKesenOtopark == eskiAd)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.FaturayiKesenOtopark, yeniAd));
            }

            kayit.Ad = yeniAd;
            kayit.Adres = form.Adres?.Trim();
            kayit.YetkiliCalisani = form.YetkiliCalisani?.Trim();
            kayit.Telefon = form.Telefon?.Trim();
            await _db.SaveChangesAsync();
            TempData["Basari"] = $"\"{yeniAd}\" güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            var kayit = await _db.Otoparklar.FindAsync(id);
            if (kayit == null) return RedirectToAction(nameof(Index));

            var kullaniliyor = await _db.Dosyalar.AnyAsync(d => d.OtoparkAdi == kayit.Ad)
                || await _db.Satislar.AnyAsync(s => s.FaturayiKesenOtopark == kayit.Ad);
            if (kullaniliyor)
            {
                TempData["Hata"] = $"\"{kayit.Ad}\" en az bir dosyada kullanılıyor, bu yüzden silinemez.";
                return RedirectToAction(nameof(Index));
            }

            _db.Otoparklar.Remove(kayit);
            await _db.SaveChangesAsync();
            TempData["Basari"] = $"\"{kayit.Ad}\" silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
