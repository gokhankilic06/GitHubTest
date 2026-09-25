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

        // Bir tarih hafta sonu (Cumartesi/Pazar) veya resmi tatil ise iş günü sayılmaz.
        private static bool IsGunuMu(DateTime tarih, ICollection<DateTime> resmiTatiller)
            => tarih.DayOfWeek != DayOfWeek.Saturday
            && tarih.DayOfWeek != DayOfWeek.Sunday
            && !resmiTatiller.Contains(tarih.Date);

        // Verilen tarihten itibaren N iş günü sonrasını bulur (hafta sonu ve resmi tatiller
        // sayılmaz). Ör: Çarşamba ihale + 3 iş günü = Perşembe, Cuma, (hafta sonu atlanır) Pazartesi.
        public static DateTime IsGunuEkle(DateTime baslangic, int isGunuSayisi, ICollection<DateTime> resmiTatiller)
        {
            var tarih = baslangic.Date;
            var sayac = 0;
            while (sayac < isGunuSayisi)
            {
                tarih = tarih.AddDays(1);
                if (IsGunuMu(tarih, resmiTatiller)) sayac++;
            }
            return tarih;
        }

        // "Yıllık toplu satış tarihi" listesi ayrıca elle girilmez: sistem zaten her hafta
        // Çarşamba satış yapıldığını ve resmi tatilleri bildiğinden, verilen tarih aralığındaki
        // (resmi tatile denk gelenler hariç) tüm Çarşambaları otomatik üretir. Komisyon İşlemleri
        // ekranındaki Satış Tarihi seçim listesi buradan gelir.
        public static List<DateTime> YilinSatisGunleri(DateTime baslangic, DateTime bitis, ICollection<DateTime> resmiTatiller)
        {
            var gunler = new List<DateTime>();
            var tarih = baslangic.Date;
            var farkGun = ((int)DayOfWeek.Wednesday - (int)tarih.DayOfWeek + 7) % 7;
            tarih = tarih.AddDays(farkGun);

            while (tarih <= bitis.Date)
            {
                if (!resmiTatiller.Contains(tarih)) gunler.Add(tarih);
                tarih = tarih.AddDays(7);
            }
            return gunler;
        }
    }
}
