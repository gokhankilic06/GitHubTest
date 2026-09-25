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

        // GET: /IptalIade - Dosya Kapama ve Satış İptali/İadesi için iki liste.
        public async Task<IActionResult> Index()
        {
            ViewBag.AktifDosyalar = await _db.Dosyalar
                .Where(d => d.Durum == DosyaDurumu.Kayitli || d.Durum == DosyaDurumu.SatisaCikti)
                .OrderByDescending(d => d.KayitTarihi)
                .ToListAsync();

            ViewBag.SatilmisDosyalar = await _db.Dosyalar
                .Include(d => d.Satislar)
                .Where(d => d.Durum == DosyaDurumu.Satildi)
                .OrderByDescending(d => d.KayitTarihi)
                .ToListAsync();

            return View();
        }

        // POST: /IptalIade/Kapat - Aktif bir dosyayı doğrudan (onay sonrası) kapatır.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Kapat(int dosyaId)
        {
            var dosya = await _db.Dosyalar.Include(d => d.IptalIade).FirstOrDefaultAsync(d => d.Id == dosyaId);
            if (dosya == null) return NotFound();

            if (dosya.IptalIade == null)
            {
                dosya.IptalIade = new IptalIade { DosyaId = dosyaId, DosyaKapatildi = true };
                _db.IptalIadeler.Add(dosya.IptalIade);
            }
            else
            {
                dosya.IptalIade.DosyaKapatildi = true;
            }
            dosya.Durum = DosyaDurumu.Kapandi;

            await _db.SaveChangesAsync();
            TempData["Basari"] = $"{dosya.DosyaNo} nolu dosya kapatıldı.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /IptalIade/Islem?dosyaId=.. - Satılmış bir dosya için iptal/iade formu.
        public async Task<IActionResult> Islem(int dosyaId)
        {
            var dosya = await _db.Dosyalar
                .Include(d => d.IptalIade)
                .Include(d => d.Satislar)
                .FirstOrDefaultAsync(d => d.Id == dosyaId);
            if (dosya == null) return NotFound();

            var satilanKayit = dosya.Satislar.FirstOrDefault(s => s.SatisSonucu == SatisSonucu.Satildi);
            ViewBag.Dosya = dosya;
            ViewBag.SatilanKayit = satilanKayit;

            var kayit = dosya.IptalIade ?? new IptalIade { DosyaId = dosyaId };
            if (kayit.IadeTutari == null && satilanKayit != null)
            {
                kayit.IadeTutari = satilanKayit.SatisBedeli;
            }
            return View(kayit);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Islem(IptalIade form)
        {
            var dosya = await _db.Dosyalar
                .Include(d => d.IptalIade)
                .Include(d => d.Satislar)
                .FirstOrDefaultAsync(d => d.Id == form.DosyaId);
            if (dosya == null) return NotFound();

            // Satış iptal ediliyorsa: o satışın (dosyayı Satıldı yapan kaydın) özeti
            // kalıcı olarak IptalIade'ye kopyalanır, ardından o Satış kaydı silinir -
            // böylece dosya, "Satış Sonuçlarını Gir" ekranında bu tür için yeniden
            // (Alıcı Çıkmadı imiş gibi) satışa çıkabilir hale gelir.
            if (form.SatisIptaliSecildi)
            {
                var satilanKayit = dosya.Satislar.FirstOrDefault(s => s.SatisSonucu == SatisSonucu.Satildi);
                if (satilanKayit != null)
                {
                    form.IptalEdilenSatisTuru = satilanKayit.SatisTuruGorunen;
                    form.IptalEdilenSatisBedeli = satilanKayit.SatisBedeli;
                    form.IptalEdilenAliciAdiSoyadi = satilanKayit.AliciAdiSoyadi;
                    if (form.IadeTutari == null) form.IadeTutari = satilanKayit.SatisBedeli;
                    _db.Satislar.Remove(satilanKayit);
                }
            }

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
                kayit.IptalEdilenSatisTuru = form.IptalEdilenSatisTuru ?? kayit.IptalEdilenSatisTuru;
                kayit.IptalEdilenSatisBedeli = form.IptalEdilenSatisBedeli ?? kayit.IptalEdilenSatisBedeli;
                kayit.IptalEdilenAliciAdiSoyadi = form.IptalEdilenAliciAdiSoyadi ?? kayit.IptalEdilenAliciAdiSoyadi;
                kayit.SatisIadesiSecildi = form.SatisIadesiSecildi;
                kayit.IadeYaziTarihi = form.IadeYaziTarihi;
                kayit.IadeYaziSayisi = form.IadeYaziSayisi;
                kayit.IadeNedeni = form.IadeNedeni;
                kayit.IadeTutari = form.IadeTutari;
                kayit.DosyaKapatildi = form.DosyaKapatildi;
                kayit.KapatmaVergiDairesiYaziTarihi = form.KapatmaVergiDairesiYaziTarihi;
                kayit.KapatmaVergiDairesiYaziSayisi = form.KapatmaVergiDairesiYaziSayisi;
            }

            // Satış iptal edildiğinde dosya PASİF değil AKTİF'e döner (yeniden satışa
            // çıkabilsin); dosya doğrudan kapatılıyorsa bu durumun önüne geçer.
            if (form.SatisIptaliSecildi) dosya.Durum = DosyaDurumu.SatisaCikti;
            if (form.DosyaKapatildi) dosya.Durum = DosyaDurumu.Kapandi;

            await _db.SaveChangesAsync();
            TempData["Basari"] = "İşlem kaydedildi.";
            return RedirectToAction("Details", "Dosya", new { id = dosya.Id });
        }
    }
}
