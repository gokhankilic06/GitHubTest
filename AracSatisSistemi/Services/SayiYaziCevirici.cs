using System.Globalization;

namespace AracSatisSistemi.Services
{
    // Parasal tutarları resmi yazılarda kullanılan Türkçe yazıya çevirir
    // (ör. 28.439,32 -> "Yirmi Sekiz Bin Dört Yüz Otuz Dokuz TL Otuz İki Kuruş").
    public static class SayiYaziCevirici
    {
        private static readonly string[] Birler = { "", "Bir", "İki", "Üç", "Dört", "Beş", "Altı", "Yedi", "Sekiz", "Dokuz" };
        private static readonly string[] Onlar = { "", "On", "Yirmi", "Otuz", "Kırk", "Elli", "Altmış", "Yetmiş", "Seksen", "Doksan" };
        private static readonly string[] Binler = { "", "Bin", "Milyon", "Milyar" };

        public static string TutarYaziyla(decimal tutar)
        {
            tutar = Math.Round(Math.Abs(tutar), 2);
            long tl = (long)Math.Floor(tutar);
            int kurus = (int)Math.Round((tutar - tl) * 100m, MidpointRounding.AwayFromZero);
            if (kurus >= 100) { tl += 1; kurus = 0; }

            string tlYazi = tl == 0 ? "Sıfır" : UcBasamakGrupYaz(tl);
            var sonuc = $"{tlYazi} TL";
            if (kurus > 0)
            {
                sonuc += $" {UcBasamakYaz(kurus)} Kuruş";
            }
            return sonuc;
        }

        private static string UcBasamakGrupYaz(long sayi)
        {
            var gruplar = new List<long>();
            long kalan = sayi;
            while (kalan > 0)
            {
                gruplar.Add(kalan % 1000);
                kalan /= 1000;
            }

            var parcalar = new List<string>();
            for (int i = gruplar.Count - 1; i >= 0; i--)
            {
                long grup = gruplar[i];
                if (grup == 0) continue;

                if (i == 1 && grup == 1)
                {
                    parcalar.Add("Bin");
                }
                else if (i > 0)
                {
                    parcalar.Add($"{UcBasamakYaz(grup)} {Binler[i]}");
                }
                else
                {
                    parcalar.Add(UcBasamakYaz(grup));
                }
            }
            return string.Join(" ", parcalar);
        }

        private static string UcBasamakYaz(long sayi)
        {
            int yuzler = (int)(sayi / 100);
            int onlarBasamagi = (int)(sayi % 100 / 10);
            int birler = (int)(sayi % 10);
            var parcalar = new List<string>();
            if (yuzler > 0)
            {
                if (yuzler > 1) parcalar.Add(Birler[yuzler]);
                parcalar.Add("Yüz");
            }
            if (onlarBasamagi > 0) parcalar.Add(Onlar[onlarBasamagi]);
            if (birler > 0) parcalar.Add(Birler[birler]);
            return string.Join(" ", parcalar);
        }
    }
}
