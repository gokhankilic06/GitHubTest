using System.ComponentModel.DataAnnotations;

namespace AracSatisSistemi.Models
{
    // Programa giriş yapabilecek kullanıcılar. Şifreler asla düz metin olarak
    // tutulmaz; Microsoft.AspNetCore.Identity.PasswordHasher ile (PBKDF2) hash'lenir.
    public class Kullanici
    {
        public int Id { get; set; }

        [Required, Display(Name = "Kullanıcı Adı")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Display(Name = "Ad Soyad")]
        public string? AdSoyad { get; set; }

        // PasswordHasher<Kullanici>.HashPassword(...) çıktısı - düz metin şifre asla saklanmaz.
        public string SifreHash { get; set; } = string.Empty;

        public bool Aktif { get; set; } = true;
    }
}
