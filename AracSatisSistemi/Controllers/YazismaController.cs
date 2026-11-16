using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;
using AracSatisSistemi.Services;

namespace AracSatisSistemi.Controllers
{
    // YAZIŞMA ekranı: dosya no girilince borçlu/alıcı/araç bilgileri ve satışla ilgili
    // mali özet (KDV, Damga Vergisi, Toplam, Otopark, Kalan Tutar) otomatik gelir.
    // Ayrıca bu bilgilerle "[Vergi Dairesi]" ve "Üst Yazı" butonlarından Word belgesi üretilir.
    public class YazismaController : Controller
    {
        private readonly AppDbContext _db;
        public YazismaController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? dosyaNo)
        {
            var tumDosyaNolar = await _db.Dosyalar.OrderBy(d => d.DosyaNo).Select(d => d.DosyaNo).ToListAsync();
            ViewBag.TumDosyaNolar = tumDosyaNolar;

            if (string.IsNullOrWhiteSpace(dosyaNo))
            {
                dosyaNo = tumDosyaNolar.LastOrDefault();
            }
            ViewBag.DosyaNo = dosyaNo;

            if (dosyaNo == null)
            {
                return View();
            }

            var veri = await SatisMaliOzetHesaplayici.Getir(_db, dosyaNo);
            if (veri == null)
            {
                TempData["Hata"] = $"\"{dosyaNo}\" numaralı dosya bulunamadı.";
                return View();
            }

            ViewBag.Dosya = veri.Dosya;
            ViewBag.Satis = veri.Satis;
            ViewBag.SatisBedeli = veri.SatisBedeli;
            ViewBag.KdvTutari = veri.KdvTutari;
            ViewBag.DvTutari = veri.DvTutari;
            ViewBag.ToplamTutar = veri.ToplamTutar;
            ViewBag.OtoparkTutari = veri.OtoparkTutari;
            ViewBag.KalanTutar = veri.KalanTutar;

            var mevcutIndex = tumDosyaNolar.IndexOf(dosyaNo);
            ViewBag.OncekiDosyaNo = mevcutIndex > 0 ? tumDosyaNolar[mevcutIndex - 1] : null;
            ViewBag.SonrakiDosyaNo = mevcutIndex >= 0 && mevcutIndex < tumDosyaNolar.Count - 1 ? tumDosyaNolar[mevcutIndex + 1] : null;

            return View();
        }

        public async Task<IActionResult> VergiDairesiYazisiIndir(string dosyaNo)
        {
            var veri = await SatisMaliOzetHesaplayici.Getir(_db, dosyaNo);
            if (veri == null)
            {
                TempData["Hata"] = $"\"{dosyaNo}\" numaralı dosya bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var belge = YazismaBelgeOlusturucu.VergiDairesiYazisiOlustur(veri.Dosya, veri.Satis, veri.KdvTutari, veri.DvTutari, veri.ToplamTutar, veri.OtoparkTutari);
            var dosyaAdi = $"{veri.Dosya.DosyaNo.Replace("/", "_")}_{veri.Dosya.VergiDairesiAdi}_Yazi.docx";
            return File(belge, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", dosyaAdi);
        }

        public async Task<IActionResult> UstYaziIndir(string dosyaNo)
        {
            var veri = await SatisMaliOzetHesaplayici.Getir(_db, dosyaNo);
            if (veri == null)
            {
                TempData["Hata"] = $"\"{dosyaNo}\" numaralı dosya bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var belge = YazismaBelgeOlusturucu.UstYaziOlustur(veri.Dosya, veri.Satis, veri.KdvTutari, veri.DvTutari, veri.ToplamTutar, veri.OtoparkTutari);
            var dosyaAdi = $"{veri.Dosya.DosyaNo.Replace("/", "_")}_UstYazi.docx";
            return File(belge, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", dosyaAdi);
        }

    }
}
