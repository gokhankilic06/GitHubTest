using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AracSatisSistemi.Models
{
    // SATIŞA İLİŞKİN BİLGİLER ekranına karşılık gelen entity.
    public class Satis
    {
        public int Id { get; set; }

        [Required]
        public int DosyaId { get; set; }
        public Dosya? Dosya { get; set; }

        [Display(Name = "Satış Türü")]
        public SatisTuru SatisTuru { get; set; }

        [Display(Name = "Satış Tarihi")]
        [DataType(DataType.Date)]
        public DateTime SatisTarihi { get; set; } = DateTime.Today;

        [Display(Name = "Satış Sonucu")]
        public SatisSonucu SatisSonucu { get; set; }

        // Yalnızca SatisSonucu = Satildi iken doldurulur (arayüzde JS ile açılır/kapanır).
        [Display(Name = "Satış Bedeli (TL)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SatisBedeli { get; set; }

        [Display(Name = "Alıcı T.C. Kimlik No")]
        public string? AliciTCKN { get; set; }

        [Display(Name = "Alıcı VKN")]
        public string? AliciVKN { get; set; }

        [Display(Name = "Alıcı Adı Soyadı")]
        public string? AliciAdiSoyadi { get; set; }

        [Display(Name = "Adres")]
        public string? Adres { get; set; }

        [Display(Name = "MTV (TL)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MTV { get; set; }

        [Display(Name = "Otoparka Giriş Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? OtoparkaGirisTarihi { get; set; }

        [Display(Name = "Otopark Günlük Ücreti (TL)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GunlukOtoparkUcreti { get; set; }

        // ---- HESAPLANAN ALANLAR ----
        // Vade: satış tarihinden itibaren 3 gün.
        [NotMapped]
        [Display(Name = "Vade")]
        public DateTime Vade => SatisTarihi.AddDays(3);

        [NotMapped]
        public int OtoparkGunSayisi => OtoparkaGirisTarihi.HasValue
            ? Math.Max((SatisTarihi.Date - OtoparkaGirisTarihi.Value.Date).Days, 0)
            : 0;

        [NotMapped]
        [Display(Name = "Otopark Ücreti (TL)")]
        public decimal OtoparkUcretiToplam => OtoparkGunSayisi * GunlukOtoparkUcreti;

        // Ekranda / yazdırmada gösterilen Türkçe metinler.
        [NotMapped]
        public string SatisTuruGorunen => SatisTuru switch
        {
            SatisTuru.BirinciSatis => "1. Satış",
            SatisTuru.IkinciSatis => "2. Satış",
            SatisTuru.Pazarlik => "Pazarlık",
            SatisTuru.Madde6183_86 => "6183/86",
            _ => SatisTuru.ToString()
        };

        [NotMapped]
        public string SonucGorunen => SatisSonucu switch
        {
            SatisSonucu.Satildi => "Satıldı",
            SatisSonucu.AliciCikmadi => "Alıcı Çıkmadı",
            _ => SatisSonucu.ToString()
        };
    }
}
