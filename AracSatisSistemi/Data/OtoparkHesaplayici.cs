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
    }
}
