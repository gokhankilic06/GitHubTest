namespace AracSatisSistemi.Models
{
    public enum SatisTuru
    {
        [System.ComponentModel.Description("1. Satış")]
        BirinciSatis = 1,
        [System.ComponentModel.Description("2. Satış")]
        IkinciSatis = 2,
        [System.ComponentModel.Description("Pazarlık")]
        Pazarlik = 3,
        [System.ComponentModel.Description("6183/86")]
        Madde6183_86 = 4
    }

    public enum SatisSonucu
    {
        [System.ComponentModel.Description("Satıldı")]
        Satildi = 1,
        [System.ComponentModel.Description("Alıcı Çıkmadı")]
        AliciCikmadi = 2
    }

    public enum DosyaDurumu
    {
        Kayitli = 0,
        SatisaCikti = 1,
        Satildi = 2,
        IptalEdildi = 3,
        IadeEdildi = 4,
        Kapandi = 5
    }

    // Otopark ücret tarifesindeki 4 araç kategorisi.
    public enum OtoparkAracKategorisi
    {
        [System.ComponentModel.Description("Küçük Araç")]
        KucukArac = 1,
        [System.ComponentModel.Description("Minibüs/Kamyonet")]
        MinibusKamyonet = 2,
        [System.ComponentModel.Description("Büyük Araç/Kamyon/Tır")]
        BuyukArac = 3,
        [System.ComponentModel.Description("Motor")]
        Motor = 4
    }

    public static class OtoparkAracKategorisiUzantilari
    {
        public static string Gorunen(this OtoparkAracKategorisi kategori) => kategori switch
        {
            OtoparkAracKategorisi.KucukArac => "Küçük Araç",
            OtoparkAracKategorisi.MinibusKamyonet => "Minibüs/Kamyonet",
            OtoparkAracKategorisi.BuyukArac => "Büyük Araç/Kamyon/Tır",
            OtoparkAracKategorisi.Motor => "Motor",
            _ => kategori.ToString()
        };
    }
}
