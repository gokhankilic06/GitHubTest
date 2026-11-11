using AracSatisSistemi.Models;

namespace AracSatisSistemi.Data
{
    // Bir dosyanın İhale Tutanağı'nda gösterilecek satış türü/tarihini belirleyen tek
    // kaynak mantık; hem tekil İhale Tutanağı sayfasında (DosyaController.IhaleTutanagi)
    // hem de Tutanak ve Raporlar'daki toplu İhale Tutanağı dökümünde (TutanakRaporController)
    // kullanılır ki ikisi arasında tutarsızlık (ör. biri doğru, diğeri boş tarih göstermesi) olmasın.
    public static class IhaleTutanagiCozucu
    {
        public record Sonuc(SatisTuru Tur, DateTime Tarih, bool TarihSeciliyor);

        public static Sonuc Coz(Dosya dosya, DateTime? secilenTarih = null)
        {
            if (dosya.Durum == DosyaDurumu.Satildi)
            {
                // Dosya zaten satıldıysa, tutanak artık ileriye dönük değil geriye dönük bir
                // kayıttır: gerçekleşen satışın tür/tarihi gösterilir.
                var satilan = dosya.Satislar.FirstOrDefault(s => s.SatisSonucu == SatisSonucu.Satildi)
                    ?? dosya.Satislar.OrderByDescending(s => s.SatisTarihi).First();
                return new Sonuc(satilan.SatisTuru, satilan.SatisTarihi, false);
            }

            // Dosyanın şu an hangi aşamayı beklediği (bkz. Dosya.Asama - tüm dallanma
            // mantığının tek kaynağı) belirler: 1./2. Satış'ın Satış Takvimi'nden gelen
            // sabit bir tarihi vardır; Pazarlık/6183-86'nın ise yoktur, bu yüzden o
            // durumda tarih elle seçilir (seçilmemişse bugüne varsayılır).
            return dosya.Asama switch
            {
                Dosya.SurecAsamasi.IkinciSatisBekliyor => new Sonuc(SatisTuru.IkinciSatis, dosya.Satis2Tarihi, false),
                Dosya.SurecAsamasi.PazarlikBekliyor => new Sonuc(SatisTuru.Pazarlik, secilenTarih ?? DateTime.Today, true),
                Dosya.SurecAsamasi.Madde6183_86Bekliyor => new Sonuc(SatisTuru.Madde6183_86, secilenTarih ?? DateTime.Today, true),
                // BirinciSatisBekliyor ve nadir görülecek diğer durumlar (AliciyaTerkBekliyor,
                // SurecTamamlandi) için varsayılan olarak 1. Satış bilgisi gösterilir.
                _ => new Sonuc(SatisTuru.BirinciSatis, dosya.Satis1Tarihi, false)
            };
        }

        // Gösterilecek tür/tarih için kaydedilmiş bir Komisyon Oturumu varsa SATIŞ KOMİSYONU
        // imza bloğunda ve saatte öncelikle o oturumun bilgileri kullanılır; hiç oturum
        // kaydedilmemişse dosyanın Kayıt anında kopyalanan eski bilgiye geri dönülür.
        //
        // "oturumlarAyniGunTur": aynı satış türü/tarihi için kaydedilmiş TÜM oturumlar (tek bir
        // tanesi değil). Pazarlık/6183-86'da aynı gün birden fazla oturum (farklı saatlerde)
        // olabileceğinden (bkz. Komisyon İşlemleri ekranı), bir saat seçilmişse (secilenSaat)
        // o saate TAM eşleşen oturum aranır; aksi halde yanlış oturumun başkan/üyeleri bu
        // dosyanın tutanağına yazılmış olurdu. 1./2. Satış'ta tarih başına tek oturum
        // beklendiğinden ilk (tek) kayıt kullanılır.
        public static (string? Baskan, string? Uye1, string? Uye2, string? Uye3, TimeSpan? Saat) KomisyonBilgisi(
            Dosya dosya, SatisTuru tur, DateTime tarih, IReadOnlyList<KomisyonOturumu> oturumlarAyniGunTur, TimeSpan? secilenSaat = null)
        {
            var oturum = secilenSaat.HasValue
                ? oturumlarAyniGunTur.FirstOrDefault(o => o.SatisSaati == secilenSaat.Value)
                : oturumlarAyniGunTur.FirstOrDefault();

            var saat = oturum != null
                ? oturum.SatisSaati
                : (tur == SatisTuru.BirinciSatis || tur == SatisTuru.IkinciSatis ? new TimeSpan(10, 0, 0) : secilenSaat);

            return (
                oturum?.Baskan ?? dosya.KomisyonBaskani,
                oturum?.Uye1 ?? dosya.KomisyonUye1,
                oturum?.Uye2 ?? dosya.KomisyonUye2,
                oturum?.Uye3 ?? dosya.KomisyonUye3,
                saat
            );
        }
    }
}
