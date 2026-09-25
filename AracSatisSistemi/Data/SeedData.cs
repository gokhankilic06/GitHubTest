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
                // Otopark ücret tarifesindeki 4 kategoriye göre varsayılan eşleme; Güncellemeler
                // ekranından her cins için gerekirse değiştirilebilir.
                var cinsler = new (string Ad, OtoparkAracKategorisi Kategori)[]
                {
                    ("Otomobil", OtoparkAracKategorisi.KucukArac),
                    ("Kamyonet", OtoparkAracKategorisi.MinibusKamyonet),
                    ("Kamyon", OtoparkAracKategorisi.BuyukArac),
                    ("Minibüs", OtoparkAracKategorisi.MinibusKamyonet),
                    ("Midibüs", OtoparkAracKategorisi.BuyukArac),
                    ("Otobüs", OtoparkAracKategorisi.BuyukArac),
                    ("Traktör", OtoparkAracKategorisi.BuyukArac),
                    ("Motosiklet", OtoparkAracKategorisi.Motor),
                    ("Çekici", OtoparkAracKategorisi.BuyukArac),
                    ("Römork", OtoparkAracKategorisi.BuyukArac),
                    ("İş Makinesi", OtoparkAracKategorisi.BuyukArac),
                    ("Diğer", OtoparkAracKategorisi.KucukArac),
                };
                context.AracCinsleri.AddRange(cinsler.Select(a => new AracCinsi { Ad = a.Ad, OtoparkKategorisi = a.Kategori }));
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
                // Paylaşılan tarife tablosundaki başlangıç değerleri; Güncellemeler ekranından
                // değiştirilebilir.
                context.GenelAyarlar.Add(new GenelAyar
                {
                    GunlukOtoparkUcreti = 0m,
                    KucukArac6AyaKadar = 83.00m,
                    KucukArac6AydanSonra = 41.50m,
                    MinibusKamyonet6AyaKadar = 91.50m,
                    MinibusKamyonet6AydanSonra = 45.75m,
                    BuyukArac6AyaKadar = 124.80m,
                    BuyukArac6AydanSonra = 62.40m,
                    Motor6AyaKadar = 41.50m,
                    Motor6AydanSonra = 20.75m,
                    CekiciUcretiSabit = 2000.00m
                });
            }

            if (!context.ResmiTatiller.Any())
            {
                // Sabit tarihli resmi/milli bayramlar - her yıl aynı gün, otomatik hesaplanır.
                // Ramazan/Kurban Bayramı gibi dini bayramlar yıldan yıla değiştiğinden burada
                // yer almaz; Güncellemeler ekranından elle eklenmelidir.
                var sabitTatilGunleri = new (int Ay, int Gun, string Aciklama)[]
                {
                    (1, 1, "Yılbaşı"),
                    (4, 23, "Ulusal Egemenlik ve Çocuk Bayramı"),
                    (5, 1, "Emek ve Dayanışma Günü"),
                    (5, 19, "Atatürk'ü Anma, Gençlik ve Spor Bayramı"),
                    (7, 15, "Demokrasi ve Milli Birlik Günü"),
                    (8, 30, "Zafer Bayramı"),
                    (10, 29, "Cumhuriyet Bayramı"),
                };

                var tatiller = new List<ResmiTatil>();
                var buYil = DateTime.Now.Year;
                for (var yil = buYil; yil <= buYil + 5; yil++)
                {
                    foreach (var (ay, gun, aciklama) in sabitTatilGunleri)
                    {
                        tatiller.Add(new ResmiTatil { Tarih = new DateTime(yil, ay, gun), Aciklama = aciklama });
                    }
                }
                context.ResmiTatiller.AddRange(tatiller);
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
