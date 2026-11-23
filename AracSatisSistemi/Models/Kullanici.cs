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

        // Admin olmayan kullanıcılar Güncellemeler ekranındaki "Kullanıcılar (Giriş
        // Yetkisi)" bölümünü (kullanıcı ekleme/silme/şifre sıfırlama, Davet Kodu) ve
        // Kullanıcılar (istatistik) sayfasını göremez.
        [Display(Name = "Admin mi?")]
        public bool Admin { get; set; }
    }

    // Bir oturumun (giriş-çıkış) kaydı: ne zaman girdi, en son ne zaman aktifti, ne
    // zaman çıktı. Kullanıcılar (istatistik) sayfasında "ne zaman giriş yaptı / kaç
    // dakika durdu" bilgisini üretmek için kullanılır (bkz. Controllers/HesapController,
    // Data/KullaniciAktiviteIzleyici).
    public class GirisKaydi
    {
        public int Id { get; set; }
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public DateTime GirisZamani { get; set; }
        public DateTime SonAktiviteZamani { get; set; }
        public DateTime? CikisZamani { get; set; }

        // Oturum süresi: çıkış yapıldıysa giriş-çıkış arası, yapılmadıysa (tarayıcı
        // kapatılmış/zaman aşımına uğramış olabilir) giriş ile bilinen son aktivite arası.
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public TimeSpan Sure => (CikisZamani ?? SonAktiviteZamani) - GirisZamani;
    }

    // Bir kullanıcının yaptığı durum değiştiren (POST) bir işlemin kaydı - Kullanıcılar
    // (istatistik) sayfasındaki "hangi işlemleri yaptı" listesi için. Sayfa görüntüleme
    // (GET) kayıt altına alınmaz, sadece ekleme/düzenleme/silme gibi işlemler.
    public class IslemKaydi
    {
        public int Id { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public DateTime Zaman { get; set; }
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Detay { get; set; }
    }
}
