using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Controllers
{
    // Satılan araçlar için alıcı kimlik ve ödeme bilgilerinin girildiği ekran.
    public class AliciOdemeController : Controller
    {
        private readonly AppDbContext _db;
        public AliciOdemeController(AppDbContext db) => _db = db;

        // GET: /AliciOdeme - Dosya No ile arama ekranı.
        public IActionResult Index(string? dosyaNo)
        {
            ViewBag.DosyaNo = dosyaNo;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Bul(string dosyaNo)
        {
            var dosya = await _db.Dosyalar
                .Include(d => d.Satislar)
                .FirstOrDefaultAsync(d => d.DosyaNo == dosyaNo);
            if (dosya == null)
            {
                TempData["Hata"] = "Bu dosya numarasına ait kayıt bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var satilanKayit = dosya.Satislar.FirstOrDefault(s => s.SatisSonucu == SatisSonucu.Satildi);
            if (satilanKayit == null)
            {
                TempData["Hata"] = "Bu dosya için henüz \"Satıldı\" sonucu girilmiş bir satış kaydı bulunmuyor. Önce Satış Sonuçlarını Gir ekranından satış bedelini giriniz.";
                return RedirectToAction(nameof(Index), new { dosyaNo });
            }

            return RedirectToAction(nameof(Gir), new { satisId = satilanKayit.Id });
        }

        // GET: /AliciOdeme/Goster?dosyaId=.. - Dosya Detayı sayfasındaki butondan doğrudan erişim.
        public async Task<IActionResult> Goster(int dosyaId)
        {
            var dosya = await _db.Dosyalar.Include(d => d.Satislar).FirstOrDefaultAsync(d => d.Id == dosyaId);
            if (dosya == null) return NotFound();

            var satilanKayit = dosya.Satislar.FirstOrDefault(s => s.SatisSonucu == SatisSonucu.Satildi);
            if (satilanKayit == null)
            {
                TempData["Hata"] = "Bu dosya için henüz \"Satıldı\" sonucu girilmiş bir satış kaydı bulunmuyor.";
                return RedirectToAction("Details", "Dosya", new { id = dosyaId });
            }
            return RedirectToAction(nameof(Gir), new { satisId = satilanKayit.Id });
        }

        // Gir formu (GET ve ModelState geçersizken POST) için ortak ViewBag hazırlığı:
        // otopark/kategori/çekici/otopark ücreti hesaplaması ve kayıtlı alıcılar listesi.
        private async Task ViewBagHazirla(Satis satis)
        {
            var genelAyar = await _db.GenelAyarlar.FirstOrDefaultAsync() ?? new GenelAyar();
            var aracCinsi = await _db.AracCinsleri.FirstOrDefaultAsync(c => c.Ad == satis.Dosya!.AracCinsi);
            var kategori = aracCinsi?.OtoparkKategorisi ?? OtoparkAracKategorisi.KucukArac;
            var otoparkUcreti = OtoparkHesaplayici.OtoparkUcretiHesapla(satis.OtoparkGunSayisi, kategori, genelAyar);
            var (ilkDonemUcret, ikinciDonemUcret) = genelAyar.OtoparkTarifesi(kategori);

            ViewBag.Dosya = satis.Dosya;
            ViewBag.Otoparklar = await _db.Otoparklar.OrderBy(o => o.Ad).ToListAsync();
            ViewBag.OtoparkKategoriAdi = aracCinsi != null
                ? kategori.Gorunen()
                : $"{kategori.Gorunen()} (varsayılan - \"{satis.Dosya!.AracCinsi}\" cinsi tanımlı değil)";
            ViewBag.OtoparkGunSayisi = satis.OtoparkGunSayisi;
            ViewBag.OtoparkUcretiHesaplanan = otoparkUcreti;
            ViewBag.OtoparkToplamHesaplanan = OtoparkHesaplayici.SonucHesapla(otoparkUcreti, satis.CekiciBedeli ?? 0m);
            ViewBag.OtoparkIlkDonemUcret = ilkDonemUcret;
            ViewBag.OtoparkIkinciDonemUcret = ikinciDonemUcret;
            ViewBag.AliciKayitlariJson = System.Text.Json.JsonSerializer.Serialize(
                await _db.AliciKayitlari.OrderBy(a => a.AdiSoyadiUnvani)
                    .Select(a => new { a.VergiNumarasi, a.AdiSoyadiUnvani, a.Vekili, a.Adresi, a.Telefon })
                    .ToListAsync());

            ViewBag.GenelAyar = genelAyar;
        }

        // GET: /AliciOdeme/Gir?satisId=..
        public async Task<IActionResult> Gir(int satisId)
        {
            var satis = await _db.Satislar.Include(s => s.Dosya).FirstOrDefaultAsync(s => s.Id == satisId);
            if (satis == null) return NotFound();

            // Verilen Süre henüz girilmemişse, ihale tarihinden sonraki 3 iş günü (hafta sonu
            // ve resmi tatiller hariç) öneri olarak hesaplanır; kullanıcı gerekirse değiştirebilir.
            if (satis.VerilenSure == null)
            {
                var resmiTatiller = await _db.ResmiTatiller.Select(t => t.Tarih.Date).ToListAsync();
                satis.VerilenSure = IhaleTarihHesaplayici.IsGunuEkle(satis.SatisTarihi, 3, resmiTatiller);
            }

            await ViewBagHazirla(satis);

            // Çekici bedeli henüz girilmemişse ve dosyada "Çekici Var" işaretliyse, sabit çekici
            // ücreti öneri olarak doldurulur; ViewBagHazirla'dan SONRA yapılır ki Toplam da buna göre hesaplansın.
            if (satis.CekiciBedeli == null && satis.Dosya!.CekiciVar)
            {
                var genelAyar = (GenelAyar)ViewBag.GenelAyar;
                satis.CekiciBedeli = genelAyar.CekiciUcretiSabit;
                ViewBag.OtoparkToplamHesaplanan = OtoparkHesaplayici.SonucHesapla((decimal)ViewBag.OtoparkUcretiHesaplanan, satis.CekiciBedeli.Value);
            }

            return View(satis);
        }

        // POST: /AliciOdeme/Gir
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Gir(int satisId, string? vergiNumarasi, Satis form)
        {
            var satis = await _db.Satislar.Include(s => s.Dosya).FirstOrDefaultAsync(s => s.Id == satisId);
            if (satis == null) return NotFound();

            vergiNumarasi = vergiNumarasi?.Trim();
            if (!string.IsNullOrEmpty(vergiNumarasi) && vergiNumarasi.Length != 10 && vergiNumarasi.Length != 11)
            {
                ModelState.AddModelError(string.Empty, "Vergi Numarası 10 haneli (VKN) veya 11 haneli (TCKN) olmalıdır.");
            }

            if (!ModelState.IsValid)
            {
                form.Id = satisId;
                form.Dosya = satis.Dosya;
                form.SatisTarihi = satis.SatisTarihi;
                await ViewBagHazirla(form);
                return View(form);
            }

            if (vergiNumarasi?.Length == 11)
            {
                satis.AliciTCKN = vergiNumarasi;
                satis.AliciVKN = null;
            }
            else if (vergiNumarasi?.Length == 10)
            {
                satis.AliciVKN = vergiNumarasi;
                satis.AliciTCKN = null;
            }
            else
            {
                satis.AliciTCKN = null;
                satis.AliciVKN = null;
            }

            satis.AliciAdiSoyadi = form.AliciAdiSoyadi;
            satis.AliciVekili = form.AliciVekili;
            satis.Adres = form.Adres;
            satis.AliciTelefon = form.AliciTelefon;
            satis.VerilenSure = form.VerilenSure;
            satis.OdemeVadesi = form.OdemeVadesi;
            satis.FaturayiKesenOtopark = form.FaturayiKesenOtopark;
            satis.OtoparkaGirisTarihi = form.OtoparkaGirisTarihi;
            satis.CekiciBedeli = form.CekiciBedeli;

            // Bu alıcı daha önce kayıtlı değilse (yeni vergi numarasıysa) alıcı listesine eklenir;
            // sonraki satışlarda "Kayıtlı Alıcılardan Seç" ile tekrar seçilebilsin.
            if (!string.IsNullOrEmpty(vergiNumarasi) && !string.IsNullOrWhiteSpace(satis.AliciAdiSoyadi))
            {
                var mevcutAlici = await _db.AliciKayitlari.FirstOrDefaultAsync(a => a.VergiNumarasi == vergiNumarasi);
                if (mevcutAlici == null)
                {
                    _db.AliciKayitlari.Add(new AliciKaydi
                    {
                        VergiNumarasi = vergiNumarasi,
                        AdiSoyadiUnvani = satis.AliciAdiSoyadi,
                        Vekili = satis.AliciVekili,
                        Adresi = satis.Adres,
                        Telefon = satis.AliciTelefon
                    });
                }
                else
                {
                    mevcutAlici.AdiSoyadiUnvani = satis.AliciAdiSoyadi;
                    mevcutAlici.Vekili = satis.AliciVekili;
                    mevcutAlici.Adresi = satis.Adres;
                    mevcutAlici.Telefon = satis.AliciTelefon;
                }
            }

            await _db.SaveChangesAsync();
            TempData["Basari"] = $"{satis.Dosya!.DosyaNo} nolu dosya için alıcı ödeme bilgileri kaydedildi.";
            return RedirectToAction("Details", "Dosya", new { id = satis.DosyaId });
        }
    }
}
