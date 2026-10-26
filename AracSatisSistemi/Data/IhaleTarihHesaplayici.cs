namespace AracSatisSistemi.Data
{
    // Haftalık Çarşamba tabanlı yardımcı hesaplamalar. 1./2. Satış tarihleri artık
    // SatisTarihiController üzerinden elle yönetilen Yıllık Satış Takvimi'nden geldiğinden,
    // buradaki Çarşamba üretimi yalnızca o takvime hızlı toplu öneri eklemek için kullanılır.
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

        // "Önümüzdeki hafta Çarşamba"yı bulur: bugünün içinde bulunduğu haftadan SONRAKİ
        // haftanın Çarşambası (bugün Çarşamba olsa bile bu haftaki değil, gelecek haftaki
        // alınır - ihale için yeterli hazırlık süresi bırakmak amacıyla). O tarih resmi
        // tatile denk gelirse bir sonraki Çarşamba'ya ertelenir (bkz. SonrakiCarsamba).
        public static DateTime OnumuzdekiHaftaCarsambasi(DateTime bugun, ICollection<DateTime> resmiTatiller)
        {
            var haftaIciGunIndex = ((int)bugun.DayOfWeek + 6) % 7; // Pazartesi=0 ... Pazar=6
            var buHaftaPazartesi = bugun.Date.AddDays(-haftaIciGunIndex);
            var onumuzdekiHaftaCarsamba = buHaftaPazartesi.AddDays(9); // +7 gün (sonraki hafta) + 2 gün (Çarşamba)
            return SonrakiCarsamba(onumuzdekiHaftaCarsamba, resmiTatiller);
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

        // Verilen tarih aralığındaki (resmi tatile denk gelenler hariç) tüm Çarşambaları
        // üretir - Satış Takvimi ekranında "Toplu Ekle" için hızlı öneri listesi olarak kullanılır.
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
