namespace AracSatisSistemi.Data
{
    // T.C. Kimlik Numarası (11 hane) ve Vergi Kimlik Numarası (10 hane) için resmi
    // kontrol hanesi (checksum) algoritmalarıyla BİÇİMSEL doğrulama yapar. Bu kontrol
    // yalnızca numaranın matematiksel olarak geçerli bir TCKN/VKN biçiminde olup
    // olmadığını (yani rastgele/yanlış girilmiş 10-11 haneli bir sayı olmadığını)
    // gösterir; numaranın GERÇEKTEN var olup olmadığını veya o kişiye/mükellefe ait
    // olup olmadığını doğrulamaz (bunun için e-Devlet/GİB sorgusu gerekir).
    public static class VknTcknDogrulayici
    {
        // TCKN algoritması (MERNİS): ilk hane 0 olamaz; 10. hane = (tek haneler
        // toplamı*7 - çift haneler toplamı) mod 10; 11. hane = ilk 10 hanenin
        // toplamının mod 10'u.
        public static bool TcknGecerliMi(string? deger)
        {
            if (string.IsNullOrWhiteSpace(deger) || deger.Length != 11 || !deger.All(char.IsAsciiDigit))
                return false;
            if (deger[0] == '0') return false;

            var d = deger.Select(c => c - '0').ToArray();
            var tekToplam = d[0] + d[2] + d[4] + d[6] + d[8];
            var ciftToplam = d[1] + d[3] + d[5] + d[7];
            var hane10 = Mod10(tekToplam * 7 - ciftToplam);
            if (hane10 != d[9]) return false;

            var hane11 = d.Take(10).Sum() % 10;
            return hane11 == d[10];
        }

        // VKN algoritması: ilk 9 hanenin her biri konumuna göre dönüştürülüp
        // toplanır, 10. (kontrol) hanesi bu toplamdan hesaplanır.
        public static bool VknGecerliMi(string? deger)
        {
            if (string.IsNullOrWhiteSpace(deger) || deger.Length != 10 || !deger.All(char.IsAsciiDigit))
                return false;

            var d = deger.Select(c => c - '0').ToArray();
            var toplam = 0;
            for (var i = 0; i < 9; i++)
            {
                var x = (d[i] + 9 - i) % 10;
                toplam += x == 9 ? 9 : (x * (1 << (9 - i))) % 9;
            }
            var kontrolHanesi = (10 - (toplam % 10)) % 10;
            return kontrolHanesi == d[9];
        }

        private static int Mod10(int deger) => ((deger % 10) + 10) % 10;
    }
}
