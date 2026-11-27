namespace AracSatisSistemi.Models
{
    // Bir Pazarlık/6183-86 Komisyon Oturumu ile, o oturumda görüşülecek dosya(lar) arasındaki
    // bağlantı - bkz. KomisyonController. 1./2. Satış'ta bu bağlantıya gerek yoktur çünkü o
    // türlerde dosya zaten Satış Takvimi'ndeki tarihle (Dosya.Satis1Tarihi/Satis2Tarihi)
    // otomatik eşleşir.
    public class KomisyonOturumuDosya
    {
        public int Id { get; set; }
        public int KomisyonOturumuId { get; set; }
        public int DosyaId { get; set; }
    }
}
