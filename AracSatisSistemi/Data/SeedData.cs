using System.Text.Json;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Data
{
    // JSON dosyalarından (Data/ReferansVerileri/*.json) okunan satırların şekli.
    public class AracCinsiVerisi
    {
        public string Ad { get; set; } = string.Empty;
        public string Kategori { get; set; } = string.Empty;
    }

    public class VergiDairesiVerisi
    {
        public string Ad { get; set; } = string.Empty;
        public string Sehir { get; set; } = string.Empty;
    }

    public class AracModeliVerisi
    {
        public string Marka { get; set; } = string.Empty;
        public List<string> Modeller { get; set; } = new();
    }

    public static class SeedData
    {
        private static readonly string VeriKlasoru = Path.Combine(AppContext.BaseDirectory, "Data", "ReferansVerileri");

        // Ayarlar ekranındaki "Tam Listeyi Yükle" gibi butonlar da aynı referans
        // verisini (ör. Türkiye genelindeki vergi daireleri) kullanabilsin diye public.
        public static List<T> JsonOku<T>(string dosyaAdi)
        {
            var yol = Path.Combine(VeriKlasoru, dosyaAdi);
            if (!File.Exists(yol)) return new List<T>();
            var json = File.ReadAllText(yol);
            return JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<T>();
        }

        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            // Araç cinsi/marka/model, vergi dairesi, otopark ve renk gibi referans listelerinin
            // ham verisi artık kod içinde değil Data/ReferansVerileri/*.json dosyalarında
            // tutuluyor. Her başlangıçta, veritabanında henüz olmayan kayıtlar bu dosyalardan
            // tamamlanır; zaten var olan (ve Güncellemeler ekranından kullanıcı tarafından
            // düzenlenmiş/silinmiş) kayıtlara dokunulmaz. Böylece hem ilk kurulum hem de daha
            // önce kısmen doldurulmuş eski bir veritabanı güvenle güncellenir, ve dosyalara
            // yeni satır eklenince (ör. yeni bir marka/model) mevcut kurulumlar da otomatik
            // yakalar.

            var mevcutCinsler = context.AracCinsleri.Select(c => c.Ad).ToHashSet();
            context.AracCinsleri.AddRange(JsonOku<AracCinsiVerisi>("AracCinsleri.json")
                .Where(c => !mevcutCinsler.Contains(c.Ad))
                .Select(c => new AracCinsi { Ad = c.Ad, OtoparkKategorisi = Enum.Parse<OtoparkAracKategorisi>(c.Kategori) }));

            var mevcutMarkalar = context.AracMarkalari.Select(m => m.Ad).ToHashSet();
            context.AracMarkalari.AddRange(JsonOku<string>("AracMarkalari.json")
                .Where(m => !mevcutMarkalar.Contains(m))
                .Select(m => new AracMarkasi { Ad = m }));

            var mevcutVergiDaireleri = context.VergiDaireleri.Select(v => v.Ad).ToHashSet();
            context.VergiDaireleri.AddRange(JsonOku<VergiDairesiVerisi>("VergiDaireleri.json")
                .Where(v => !mevcutVergiDaireleri.Contains(v.Ad))
                .Select(v => new VergiDairesiKaydi { Ad = v.Ad, Sehir = v.Sehir }));

            var mevcutOtoparklar = context.Otoparklar.Select(o => o.Ad).ToHashSet();
            context.Otoparklar.AddRange(JsonOku<string>("Otoparklar.json")
                .Where(o => !mevcutOtoparklar.Contains(o))
                .Select(o => new OtoparkKaydi { Ad = o }));

            var mevcutRenkler = context.AracRenkleri.Select(r => r.Ad).ToHashSet();
            context.AracRenkleri.AddRange(JsonOku<string>("AracRenkleri.json")
                .Where(r => !mevcutRenkler.Contains(r))
                .Select(r => new AracRengi { Ad = r }));

            // Araç Modelleri marka bazlı tutulduğundan (MarkaAdi + Ad birlikte benzersiz)
            // eşleşme iki alanın birleşimiyle yapılır.
            var mevcutModeller = context.AracModelleri.Select(m => m.MarkaAdi + "|" + m.Ad).ToHashSet();
            context.AracModelleri.AddRange(JsonOku<AracModeliVerisi>("AracModelleri.json")
                .SelectMany(m => m.Modeller.Select(ad => new AracModeli { MarkaAdi = m.Marka, Ad = ad }))
                .Where(m => !mevcutModeller.Contains(m.MarkaAdi + "|" + m.Ad)));

            if (!context.KomisyonAyarlari.Any())
            {
                context.KomisyonAyarlari.Add(new KomisyonAyari
                {
                    Baskan = "Komisyon Başkanı (güncelleyin)",
                    Uye1 = "Üye 1 (güncelleyin)",
                    Uye2 = "Üye 2 (güncelleyin)",
                    Uye3 = "Üye 3 (güncelleyin)"
                });
            }

            if (!context.GenelAyarlar.Any())
            {
                // Paylaşılan tarife tablosundaki başlangıç değerleri; Güncellemeler ekranından
                // değiştirilebilir.
                context.GenelAyarlar.Add(new GenelAyar
                {
                    KucukArac6AyaKadar = 83.00m,
                    KucukArac6AydanSonra = 41.50m,
                    MinibusKamyonet6AyaKadar = 91.50m,
                    MinibusKamyonet6AydanSonra = 45.75m,
                    BuyukArac6AyaKadar = 124.80m,
                    BuyukArac6AydanSonra = 62.40m,
                    Motor6AyaKadar = 41.50m,
                    Motor6AydanSonra = 20.75m,
                    KucukAracCekiciUcreti = 2000.00m,
                    MinibusKamyonetCekiciUcreti = 2000.00m,
                    BuyukAracCekiciUcreti = 5000.00m,
                    MotorCekiciUcreti = 2000.00m,
                    MaksimumModelYili = 2027
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
