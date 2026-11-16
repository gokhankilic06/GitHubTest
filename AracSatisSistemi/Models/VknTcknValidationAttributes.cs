using System.ComponentModel.DataAnnotations;
using AracSatisSistemi.Data;

namespace AracSatisSistemi.Models
{
    // Alan boşsa (opsiyonel) geçerli sayılır - doluluk zorunluluğu ayrı [Required]/
    // [StringLength] ile kontrol edilir; bu öznitelik yalnızca DOLU bir değerin
    // gerçek bir TCKN/VKN kontrol hanesine uyup uymadığına bakar.
    public class GecerliTCKNAttribute : ValidationAttribute
    {
        public GecerliTCKNAttribute() : base("Geçerli bir T.C. Kimlik Numarası giriniz (kontrol hanesi tutmuyor).") { }

        public override bool IsValid(object? value)
        {
            if (value is not string s || string.IsNullOrWhiteSpace(s)) return true;
            return VknTcknDogrulayici.TcknGecerliMi(s);
        }
    }

    public class GecerliVKNAttribute : ValidationAttribute
    {
        public GecerliVKNAttribute() : base("Geçerli bir Vergi Kimlik Numarası giriniz (kontrol hanesi tutmuyor).") { }

        public override bool IsValid(object? value)
        {
            if (value is not string s || string.IsNullOrWhiteSpace(s)) return true;
            return VknTcknDogrulayici.VknGecerliMi(s);
        }
    }
}
