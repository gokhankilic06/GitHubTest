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
    // Giriş/çıkış işlemleri. Tüm diğer controller'lar Program.cs'teki FallbackPolicy
    // sayesinde otomatik olarak [Authorize]; bu controller [AllowAnonymous] olduğundan
    // giriş yapmamış kullanıcı yalnızca buraya erişebilir.
    [AllowAnonymous]
    public class HesapController : Controller
    {
        private readonly AppDbContext _db;
        public HesapController(AppDbContext db) => _db = db;

        public IActionResult Giris(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, kullanici!.KullaniciAdi),
                new Claim("AdSoyad", kullanici.AdSoyad ?? kullanici.KullaniciAdi),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cikis()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Giris));
        }

        // Giriş yapmış herhangi bir kullanıcının kendi şifresini değiştirmesi için.
        [Authorize]
        public IActionResult SifreDegistir() => View();

        [HttpPost, ValidateAntiForgeryToken, Authorize]
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
