namespace AracSatisSistemi.Models
{
    // _IhaleTutanagiIcerik partial'ının ihtiyaç duyduğu tüm veriyi (dosya + o dosya için
    // çözülmüş satış türü/tarihi/komisyon bilgisi) bir arada taşır; hem tekil İhale
    // Tutanağı sayfasında hem de Tutanak ve Raporlar'daki toplu dökümde kullanılır.
    public class IhaleTutanagiGorunumu
    {
        public Dosya Dosya { get; set; } = null!;
        public SatisTuru GosterilecekTur { get; set; }
        public DateTime GosterilecekTarih { get; set; }
        public TimeSpan? SatisSaati { get; set; }
        public bool TarihSeciliyor { get; set; }
        public string? KomisyonBaskani { get; set; }
        public string? KomisyonUye1 { get; set; }
        public string? KomisyonUye2 { get; set; }
        public string? KomisyonUye3 { get; set; }
    }
}
