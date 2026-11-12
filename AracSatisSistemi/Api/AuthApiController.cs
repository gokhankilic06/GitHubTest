using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Api
{
    public record GirisIstegi(string KullaniciAdi, string Sifre);
    public record GirisYaniti(string Token, DateTime GecerlilikSonu, string KullaniciAdi, string AdSoyad);

    // React arayüzünün giriş yapması için JWT (bearer token) üreten uç nokta. Aynı
    // Kullanicilar tablosunu ve aynı şifre hash'ini (PasswordHasher) Razor arayüzündeki
    // Hesap/Giris ile paylaşır; tek bir kullanıcı/şifre listesi iki arayüzde de geçerlidir.
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    public class AuthApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        public AuthApiController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("giris")]
        public async Task<ActionResult<GirisYaniti>> Giris(GirisIstegi istek)
        {
            var kullaniciAdi = (istek.KullaniciAdi ?? string.Empty).Trim();
            var kullanici = await _db.Kullanicilar.FirstOrDefaultAsync(k => k.KullaniciAdi == kullaniciAdi && k.Aktif);
            var hasher = new PasswordHasher<Kullanici>();
            var gecerliMi = kullanici != null &&
                hasher.VerifyHashedPassword(kullanici, kullanici.SifreHash, istek.Sifre ?? string.Empty) != PasswordVerificationResult.Failed;

            if (!gecerliMi)
            {
                return Unauthorized(new { mesaj = "Kullanıcı adı veya şifre hatalı." });
            }

            var jwt = _config.GetSection("Jwt");
            var gecerlilikSonu = DateTime.UtcNow.AddHours(12);
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, kullanici!.KullaniciAdi),
                new Claim("AdSoyad", kullanici.AdSoyad ?? kullanici.KullaniciAdi),
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: gecerlilikSonu,
                signingCredentials: creds);

            return new GirisYaniti(
                new JwtSecurityTokenHandler().WriteToken(token),
                gecerlilikSonu,
                kullanici.KullaniciAdi,
                kullanici.AdSoyad ?? kullanici.KullaniciAdi);
        }
    }
}
