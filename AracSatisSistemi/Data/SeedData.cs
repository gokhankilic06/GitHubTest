using AracSatisSistemi.Models;

namespace AracSatisSistemi.Data
{
    public static class SeedData
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            if (!context.AracCinsleri.Any())
            {
                var cinsler = new[]
                {
                    "Otomobil", "Kamyonet", "Kamyon", "Minibüs", "Midibüs", "Otobüs",
                    "Traktör", "Motosiklet", "Çekici", "Römork", "İş Makinesi", "Diğer"
                };
                context.AracCinsleri.AddRange(cinsler.Select(a => new AracCinsi { Ad = a }));
            }

            if (!context.AracMarkalari.Any())
            {
                // NOT: Bu liste düzenlenebilir bir başlangıç listesidir. Tam ve güncel marka
                // listesi için Güncellemeler ekranından https://www.tsb.org.tr/tr/kasko-deger-listesi
                // adresindeki liste referans alınarak eklenmelidir (bu ortamda internet erişimi
                // olmadığından otomatik çekim yapılamamıştır).
                var markalar = new[]
                {
                    "Renault", "Fiat", "Ford", "Volkswagen", "Toyota", "Hyundai", "Opel",
                    "Peugeot", "Citroen", "Dacia", "Honda", "Nissan", "Mercedes-Benz", "BMW",
                    "Audi", "Skoda", "Seat", "Kia", "Chevrolet", "Mazda", "Suzuki",
                    "Mitsubishi", "Volvo", "Land Rover", "Jeep", "Isuzu", "Iveco", "MAN",
                    "Scania", "DAF", "BMC", "Otokar", "TEMSA", "Yamaha", "Honda Motosiklet",
                    "TOGG", "Diğer"
                };
                context.AracMarkalari.AddRange(markalar.Select(a => new AracMarkasi { Ad = a }));
            }

            if (!context.VergiDaireleri.Any())
            {
                context.VergiDaireleri.AddRange(TumTurkiyeVergiDaireleri.Liste);
            }

            if (!context.Otoparklar.Any())
            {
                // Ankara'daki yediemin otoparkları - örnek/başlangıç listesi.
                var otoparklar = new[]
                {
                    "Ostim Yediemin Otoparkı", "Sincan Yediemin Otoparkı",
                    "Mamak Yediemin Otoparkı", "Çubuk Yediemin Otoparkı",
                    "Polatlı Yediemin Otoparkı"
                };
                context.Otoparklar.AddRange(otoparklar.Select(a => new OtoparkKaydi { Ad = a }));
            }

            if (!context.AracRenkleri.Any())
            {
                var renkler = new[]
                {
                    "Beyaz", "Siyah", "Gri", "Gümüş", "Kırmızı", "Mavi", "Lacivert",
                    "Yeşil", "Sarı", "Turuncu", "Kahverengi", "Bej", "Bordo", "Mor",
                    "Pembe", "Altın", "Turkuaz", "Diğer"
                };
                context.AracRenkleri.AddRange(renkler.Select(a => new AracRengi { Ad = a }));
            }

            if (!context.KomisyonAyarlari.Any())
            {
                context.KomisyonAyarlari.Add(new KomisyonAyari
                {
                    Baskan = "Komisyon Başkanı (güncelleyin)",
                    Uye1 = "Üye 1 (güncelleyin)",
                    Uye2 = "Üye 2 (güncelleyin)",
                    Uye3 = "Üye 3 (güncelleyin)",
                    Uye4 = "Üye 4 (güncelleyin)"
                });
            }

            if (!context.GenelAyarlar.Any())
            {
                context.GenelAyarlar.Add(new GenelAyar { GunlukOtoparkUcreti = 0m });
            }

            context.SaveChanges();
        }

        // Dosya no formatı: YIL/000001 (yıl içinde artan sıra numarası)
        public static string YeniDosyaNoUret(AppDbContext context)
        {
            int yil = DateTime.Now.Year;
            string prefix = $"{yil}/";
            var sonSira = context.Dosyalar
                .Where(d => d.DosyaNo.StartsWith(prefix))
                .Select(d => d.DosyaNo)
                .AsEnumerable()
                .Select(no =>
                {
                    var parts = no.Split('/');
                    return parts.Length == 2 && int.TryParse(parts[1], out var n) ? n : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            return $"{prefix}{(sonSira + 1):D4}";
        }
    }
}
