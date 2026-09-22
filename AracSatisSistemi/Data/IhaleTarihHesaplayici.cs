namespace AracSatisSistemi.Data
{
    // İhale (satış) tarihleri her hafta Çarşamba günü olacak şekilde otomatik hesaplanır.
    public static class IhaleTarihHesaplayici
    {
        // Verilen tarihten itibaren (o tarih dahil) ilk Çarşamba'yı bulur; resmi tatile
        // denk gelirse tatil olmayan bir sonraki Çarşamba'ya ertelenir.
        public static DateTime SonrakiCarsamba(DateTime baslangic, ICollection<DateTime> resmiTatiller)
        {
            var tarih = baslangic.Date;
            var farkGun = ((int)DayOfWeek.Wednesday - (int)tarih.DayOfWeek + 7) % 7;
            tarih = tarih.AddDays(farkGun);

            while (resmiTatiller.Contains(tarih))
            {
                tarih = tarih.AddDays(7);
            }
            return tarih;
        }

        // 1. Satış: kayıt tarihinden sonraki ilk (tatil olmayan) Çarşamba.
        // 2. Satış: 1. Satıştan bir sonraki (tatil olmayan) Çarşamba.
        public static (DateTime satis1, DateTime satis2) PlanliSatisTarihleriHesapla(DateTime kayitTarihi, ICollection<DateTime> resmiTatiller)
        {
            var satis1 = SonrakiCarsamba(kayitTarihi, resmiTatiller);
            var satis2 = SonrakiCarsamba(satis1.AddDays(1), resmiTatiller);
            return (satis1, satis2);
        }
    }
}
