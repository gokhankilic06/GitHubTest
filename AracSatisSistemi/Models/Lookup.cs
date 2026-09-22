using System.ComponentModel.DataAnnotations;

namespace AracSatisSistemi.Models
{
    // "Güncellemeler" ekranından yönetilebilen basit referans listeleri.
    // Bu sayede araç markası / cinsi / vergi dairesi / otopark listeleri
    // koda dokunmadan eklenip çıkarılabilir.

    public class AracMarkasi
    {
        public int Id { get; set; }
        [Required] public string Ad { get; set; } = string.Empty;
    }

    public class AracCinsi
    {
        public int Id { get; set; }
        [Required] public string Ad { get; set; } = string.Empty;
    }

    public class VergiDairesiKaydi
    {
        public int Id { get; set; }
        [Required] public string Ad { get; set; } = string.Empty;
        [Required] public string Sehir { get; set; } = string.Empty;
    }

    public class OtoparkKaydi
    {
        public int Id { get; set; }
        [Required] public string Ad { get; set; } = string.Empty;
    }

    public class AracRengi
    {
        public int Id { get; set; }
        [Required] public string Ad { get; set; } = string.Empty;
    }

    // Komisyon üyeleri - tek satırlık ayar; her yeni dosyada bu bilgiler
    // otomatik gelir, güncelleme yetkisi olan kullanıcı burayı değiştirebilir.
    public class KomisyonAyari
    {
        public int Id { get; set; }
        [Display(Name = "Komisyon Başkanı")] public string Baskan { get; set; } = string.Empty;
        [Display(Name = "Üye 1")] public string Uye1 { get; set; } = string.Empty;
        [Display(Name = "Üye 2")] public string Uye2 { get; set; } = string.Empty;
        [Display(Name = "Üye 3")] public string Uye3 { get; set; } = string.Empty;
        [Display(Name = "Üye 4")] public string Uye4 { get; set; } = string.Empty;
    }

    // Genel sistem ayarları (günlük otopark ücreti gibi elden verilen tarifeler)
    public class GenelAyar
    {
        public int Id { get; set; }
        [Display(Name = "Günlük Otopark Ücreti (TL)")]
        public decimal GunlukOtoparkUcreti { get; set; }
    }
}
