using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AracSatisSistemi.Models
{
    // KOMİSYON ÜYE GİRİŞ İŞLEMLERİ ekranına karşılık gelen entity: belirli bir satış
    // türü/tarihi/saatinde ihaleyi hangi komisyonun (başkan + 3 üye) yürüteceğini kaydeder.
    // Pazarlık ve 6183/86 gibi aynı gün birden fazla kez yapılabilen satışları birbirinden
    // ayırt edebilmek için Satış Saati de tutulur.
    public class KomisyonOturumu
    {
        public int Id { get; set; }

        [Display(Name = "Satış Türü")]
        public SatisTuru SatisTuru { get; set; }

        [Display(Name = "Satış Tarihi")]
        [DataType(DataType.Date)]
        public DateTime SatisTarihi { get; set; }

        [Display(Name = "Satış Saati")]
        [DataType(DataType.Time)]
        public TimeSpan SatisSaati { get; set; }

        [Required, Display(Name = "Komisyon Başkanı")]
        public string Baskan { get; set; } = string.Empty;
        [Required, Display(Name = "1 Nolu Üye")]
        public string Uye1 { get; set; } = string.Empty;
        [Required, Display(Name = "2 Nolu Üye")]
        public string Uye2 { get; set; } = string.Empty;
        [Required, Display(Name = "3 Nolu Üye")]
        public string Uye3 { get; set; } = string.Empty;

        [NotMapped]
        public string SatisTuruGorunen => SatisTuru switch
        {
            SatisTuru.BirinciSatis => "1. Satış",
            SatisTuru.IkinciSatis => "2. Satış",
            SatisTuru.Pazarlik => "Pazarlık",
            SatisTuru.Madde6183_86 => "6183/86 Mad.",
            _ => SatisTuru.ToString()
        };
    }
}
