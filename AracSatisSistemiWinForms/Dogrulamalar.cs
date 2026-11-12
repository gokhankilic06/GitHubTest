using System.Text.RegularExpressions;

namespace AracSatisSistemiWinForms
{
    // Plaka / VKN / TCKN biçim doğrulamaları - sunucudaki (Models/Dosya.cs,
    // Data/VknTcknDogrulayici.cs) ile BİREBİR aynı kurallar, kullanıcıya formu
    // göndermeden önce anlık geri bildirim verebilmek için.
    public static class Dogrulamalar
    {
        private static readonly Regex PlakaDeseni = new(
            @"^(0[1-9]|[1-7][0-9]|8[01])([A-PR-VYZ]\d{4}|[A-PR-VYZ]{2}\d{3,4}|[A-PR-VYZ]{3}\d{2})$",
            RegexOptions.Compiled);

        public static bool PlakaGecerliMi(string deger) => PlakaDeseni.IsMatch(deger);

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
