using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Controllers
{
    // Giriş/çıkış işlemleri. Tüm diğer controller'lar (ve bu controller'daki Çıkış/Şifre
    // Değiştir dahil) Program.cs'teki FallbackPolicy sayesinde otomatik olarak [Authorize];
    // yalnızca Giriş ekranı [AllowAnonymous] olduğundan giriş yapmamış kullanıcı oraya
    // erişebilir. [AllowAnonymous] kasıtlı olarak sınıf değil, yalnızca Giriş action'larına
    // uygulanır: sınıf genelinde olsaydı Şifre Değiştir/Çıkış da (ASP0026 analizörünün de
    // işaret ettiği gibi) yanlışlıkla kimlik doğrulamasız erişilebilir hale gelirdi.
    public class HesapController : Controller
    {
        private readonly AppDbContext _db;
        public HesapController(AppDbContext db) => _db = db;

        [AllowAnonymous]
        public IActionResult Giris(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public async Task<IActionResult> Giris(string kullaniciAdi, string sifre, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            kullaniciAdi = (kullaniciAdi ?? string.Empty).Trim();

            var kullanici = await _db.Kullanicilar.FirstOrDefaultAsync(k => k.KullaniciAdi == kullaniciAdi && k.Aktif);
            var hasher = new PasswordHasher<Kullanici>();
            var girisGecerliMi = kullanici != null &&
                hasher.VerifyHashedPassword(kullanici, kullanici.SifreHash, sifre ?? string.Empty) != PasswordVerificationResult.Failed;

            if (!girisGecerliMi)
            {
                ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
                ViewBag.KullaniciAdi = kullaniciAdi;
                return View();
            }

            await GirisYapAsync(kullanici!);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        // Oturum açma (cookie + Admin/GirisKaydiId claim'leri) ve GirisKaydi oluşturma -
        // hem normal Giriş hem Kayıt Ol sonrası ortak akış; bkz. Kullanıcılar (istatistik)
        // sayfası, Data/KullaniciAktiviteIzleyici.
        private async Task GirisYapAsync(Kullanici kullanici)
        {
            var simdi = DateTime.Now;
            var girisKaydi = new GirisKaydi
            {
                KullaniciId = kullanici.Id,
                KullaniciAdi = kullanici.KullaniciAdi,
                GirisZamani = simdi,
                SonAktiviteZamani = simdi
            };
            _db.GirisKayitlari.Add(girisKaydi);
            await _db.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, kullanici.KullaniciAdi),
                new Claim("AdSoyad", kullanici.AdSoyad ?? kullanici.KullaniciAdi),
                new Claim("GirisKaydiId", girisKaydi.Id.ToString()),
            };
            if (kullanici.Admin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            // IsPersistent = false: oturum çerezi tarayıcı tamamen kapatılınca sona erer,
            // böylece siteye tekrar girildiğinde eski kullanıcıyla otomatik oturum açılmaz.
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false });
        }

        // Yeni kullanıcıların admin onayı almadan, sadece Güncellemeler'de belirlenen davet
        // kodunu bilerek kendi hesaplarını oluşturabilmesi için. GenelAyar.DavetKodu boşsa
        // (admin hiç kod belirlemediyse) ekran tamamen kapalı kalır.
        [AllowAnonymous]
        public async Task<IActionResult> KayitOl()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            var genelAyar = await _db.GenelAyarlar.FirstOrDefaultAsync();
            ViewBag.KayitAcikMi = !string.IsNullOrWhiteSpace(genelAyar?.DavetKodu);
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public async Task<IActionResult> KayitOl(string kullaniciAdi, string adSoyad, string sifre, string sifreTekrar, string davetKodu)
        {
            var genelAyar = await _db.GenelAyarlar.FirstOrDefaultAsync();
            ViewBag.KayitAcikMi = !string.IsNullOrWhiteSpace(genelAyar?.DavetKodu);
            if (!ViewBag.KayitAcikMi)
            {
                TempData["Hata"] = "Kayıt Ol ekranı şu anda kapalı; yöneticinizden Güncellemeler ekranından bir davet kodu belirlemesini isteyin.";
                return RedirectToAction(nameof(Giris));
            }

            kullaniciAdi = (kullaniciAdi ?? string.Empty).Trim();
            ViewBag.KullaniciAdi = kullaniciAdi;
            ViewBag.AdSoyad = adSoyad;

            if (davetKodu != genelAyar!.DavetKodu)
            {
                ModelState.AddModelError(string.Empty, "Davet kodu hatalı.");
                return View();
            }
            if (string.IsNullOrWhiteSpace(kullaniciAdi))
            {
                ModelState.AddModelError(string.Empty, "Kullanıcı adı zorunludur.");
                return View();
            }
            if (await _db.Kullanicilar.AnyAsync(k => k.KullaniciAdi == kullaniciAdi))
            {
                ModelState.AddModelError(string.Empty, $"\"{kullaniciAdi}\" kullanıcı adı zaten alınmış.");
                return View();
            }
            if (string.IsNullOrWhiteSpace(sifre) || sifre.Length < 6)
            {
                ModelState.AddModelError(string.Empty, "Şifre en az 6 karakter olmalıdır.");
                return View();
            }
            if (sifre != sifreTekrar)
            {
                ModelState.AddModelError(string.Empty, "Şifre ve tekrarı birbiriyle uyuşmuyor.");
                return View();
            }

            var yeniKullanici = new Kullanici { KullaniciAdi = kullaniciAdi, AdSoyad = string.IsNullOrWhiteSpace(adSoyad) ? null : adSoyad.Trim() };
            var hasher = new PasswordHasher<Kullanici>();
            yeniKullanici.SifreHash = hasher.HashPassword(yeniKullanici, sifre);
            _db.Kullanicilar.Add(yeniKullanici);
            await _db.SaveChangesAsync();

            TempData["Basari"] = "Kaydınız başarıyla oluşturuldu. Şimdi giriş yapabilirsiniz.";
            return RedirectToAction(nameof(Giris));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cikis()
        {
            var girisKaydiIdClaim = User.FindFirst("GirisKaydiId")?.Value;
            if (int.TryParse(girisKaydiIdClaim, out var girisKaydiId))
            {
                var kayit = await _db.GirisKayitlari.FindAsync(girisKaydiId);
                if (kayit != null && kayit.CikisZamani == null)
                {
                    kayit.CikisZamani = DateTime.Now;
                    await _db.SaveChangesAsync();
                }
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Giris));
        }

        // Giriş yapmış herhangi bir kullanıcının kendi şifresini değiştirmesi için.
        // Ayrı bir [Authorize] gerekmez; global FallbackPolicy (Program.cs) zaten
        // [AllowAnonymous] işaretlenmemiş her action için kimlik doğrulaması zorunlu kılar.
        public IActionResult SifreDegistir() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SifreDegistir(string mevcutSifre, string yeniSifre, string yeniSifreTekrar)
        {
            var kullanici = await _db.Kullanicilar.FirstOrDefaultAsync(k => k.KullaniciAdi == User.Identity!.Name);
            if (kullanici == null) return RedirectToAction(nameof(Giris));

            var hasher = new PasswordHasher<Kullanici>();
            if (hasher.VerifyHashedPassword(kullanici, kullanici.SifreHash, mevcutSifre ?? string.Empty) == PasswordVerificationResult.Failed)
            {
                TempData["Hata"] = "Mevcut şifreniz hatalı.";
                return RedirectToAction(nameof(SifreDegistir));
            }
            if (string.IsNullOrWhiteSpace(yeniSifre) || yeniSifre.Length < 6)
            {
                TempData["Hata"] = "Yeni şifre en az 6 karakter olmalıdır.";
                return RedirectToAction(nameof(SifreDegistir));
            }
            if (yeniSifre != yeniSifreTekrar)
            {
                TempData["Hata"] = "Yeni şifre ve tekrarı birbiriyle uyuşmuyor.";
                return RedirectToAction(nameof(SifreDegistir));
            }

            kullanici.SifreHash = hasher.HashPassword(kullanici, yeniSifre);
            await _db.SaveChangesAsync();
            TempData["Basari"] = "Şifreniz değiştirildi.";
            return RedirectToAction("Index", "Home");
        }
    }
}
