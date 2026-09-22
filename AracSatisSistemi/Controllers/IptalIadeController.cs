using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Controllers
{
    public class IptalIadeController : Controller
    {
        private readonly AppDbContext _db;
        public IptalIadeController(AppDbContext db) => _db = db;

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Bul(string dosyaNo)
        {
            var dosya = await _db.Dosyalar
                .Include(d => d.IptalIade)
                .FirstOrDefaultAsync(d => d.DosyaNo == dosyaNo);

            if (dosya == null)
            {
                TempData["Hata"] = "Bu dosya numarasına ait kayıt bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Islem), new { dosyaId = dosya.Id });
        }

        public async Task<IActionResult> Islem(int dosyaId)
        {
            var dosya = await _db.Dosyalar
                .Include(d => d.IptalIade)
                .FirstOrDefaultAsync(d => d.Id == dosyaId);
            if (dosya == null) return NotFound();

            var kayit = dosya.IptalIade ?? new IptalIade { DosyaId = dosyaId };
            ViewBag.Dosya = dosya;
            return View(kayit);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Islem(IptalIade form)
        {
            var dosya = await _db.Dosyalar
                .Include(d => d.IptalIade)
                .FirstOrDefaultAsync(d => d.Id == form.DosyaId);
            if (dosya == null) return NotFound();

            if (dosya.IptalIade == null)
            {
                dosya.IptalIade = form;
                _db.IptalIadeler.Add(form);
            }
            else
            {
                var kayit = dosya.IptalIade;
                kayit.SatisIptaliSecildi = form.SatisIptaliSecildi;
                kayit.IptalYaziTarihi = form.IptalYaziTarihi;
                kayit.IptalYaziSayisi = form.IptalYaziSayisi;
                kayit.IptalNedeni = form.IptalNedeni;
                kayit.SatisIadesiSecildi = form.SatisIadesiSecildi;
                kayit.IadeYaziTarihi = form.IadeYaziTarihi;
                kayit.IadeYaziSayisi = form.IadeYaziSayisi;
                kayit.IadeNedeni = form.IadeNedeni;
                kayit.DosyaKapatildi = form.DosyaKapatildi;
                kayit.KapatmaVergiDairesiYaziTarihi = form.KapatmaVergiDairesiYaziTarihi;
                kayit.KapatmaVergiDairesiYaziSayisi = form.KapatmaVergiDairesiYaziSayisi;
            }

            if (form.SatisIptaliSecildi) dosya.Durum = DosyaDurumu.IptalEdildi;
            if (form.SatisIadesiSecildi) dosya.Durum = DosyaDurumu.IadeEdildi;
            if (form.DosyaKapatildi) dosya.Durum = DosyaDurumu.Kapandi;

            await _db.SaveChangesAsync();
            TempData["Basari"] = "İşlem kaydedildi.";
            return RedirectToAction("Details", "Dosya", new { id = dosya.Id });
        }
    }
}
