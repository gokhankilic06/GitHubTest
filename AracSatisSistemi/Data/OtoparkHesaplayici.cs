using AracSatisSistemi.Models;

namespace AracSatisSistemi.Data
{
    // Araç kategorisi ve otoparkta kalınan gün sayısına göre kademeli otopark ücreti hesabı.
    // İlk 180 gün (6 ay) "6 Aya Kadar" günlük ücreti, kalan günler "6 Aydan Sonra" (genelde
    // ilkin yarısı) günlük ücreti üzerinden hesaplanır.
    public static class OtoparkHesaplayici
    {
        private const int AltiAyGunSayisi = 180;

        public static decimal OtoparkUcretiHesapla(int gunSayisi, OtoparkAracKategorisi kategori, GenelAyar tarife)
        {
            if (gunSayisi <= 0) return 0m;

            var (ilkDonemUcret, ikinciDonemUcret) = tarife.OtoparkTarifesi(kategori);

            if (gunSayisi <= AltiAyGunSayisi)
            {
                return gunSayisi * ilkDonemUcret;
            }

            var ilkDonemToplam = AltiAyGunSayisi * ilkDonemUcret;
            var ikinciDonemToplam = (gunSayisi - AltiAyGunSayisi) * ikinciDonemUcret;
            return ilkDonemToplam + ikinciDonemToplam;
        }

        // Otopark ücreti + çekici ücretinin (varsa) tam sayıya yuvarlanmış toplamı.
        public static decimal SonucHesapla(decimal otoparkUcreti, decimal cekiciUcreti)
            => Math.Round(otoparkUcreti + cekiciUcreti, 0, MidpointRounding.AwayFromZero);

        // İdare talimatı: otopark ücreti + çekici ücreti toplamı, satış bedelinin %30'unu
        // aşamaz. Çekici ücreti sabit tarife tutarı olarak kabul edilip bu sınırdan düşülür;
        // kalan kısım otopark ücretinin üst sınırını oluşturur. Satış bedeli henüz belli
        // değilse (satış sonucu girilmemiş) sınır uygulanmaz.
        public const decimal OtoparkCekiciTavanOrani = 0.30m;

        public static decimal OtoparkTavaniUygula(decimal hamOtoparkUcreti, decimal cekiciUcreti, decimal? satisBedeli)
        {
            if (!satisBedeli.HasValue) return hamOtoparkUcreti;
            var tavan = satisBedeli.Value * OtoparkCekiciTavanOrani;
            var otoparkIcinKalanTavan = Math.Max(0m, tavan - cekiciUcreti);
            return Math.Min(hamOtoparkUcreti, otoparkIcinKalanTavan);
        }

        // Bir Satış kaydının GERÇEK (kademeli, araç kategorisine göre, %30 tavanı uygulanmış)
        // otopark ücreti. Alıcı Ödeme Bilgileri / Satış Sonucu Gir / Yazışma ekranlarındaki
        // hesaplamayla tutarlı tek sonuç üretmesi için Excel/Access yedekleme ve Dosya
        // Detayı gibi raporlama noktalarında da Satis.OtoparkUcretiToplam (eski, düz günlük
        // ücretli hesaplama) yerine bu metot kullanılmalıdır.
        public static decimal GercekOtoparkUcreti(Satis satis, string? aracCinsi, IReadOnlyDictionary<string, OtoparkAracKategorisi> kategoriSozlugu, GenelAyar tarife)
        {
            var kategori = aracCinsi != null && kategoriSozlugu.TryGetValue(aracCinsi, out var k)
                ? k
                : OtoparkAracKategorisi.KucukArac;
            var hamOtopark = OtoparkUcretiHesapla(satis.OtoparkGunSayisi, kategori, tarife);
            return OtoparkTavaniUygula(hamOtopark, satis.CekiciBedeli ?? 0m, satis.SatisBedeli);
        }
    }
}
