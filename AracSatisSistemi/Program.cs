using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;

// Uygulama tamamen Türkçe (tarih/sayı biçimleri: 300.000,00 gibi) olduğundan varsayılan
// kültür sunucunun işletim sistemi ayarından bağımsız olarak sabit tr-TR yapılır.
var trKultur = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = trKultur;
CultureInfo.DefaultThreadCurrentUICulture = trKultur;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // bkz. InvariantDecimalModelBinder: tr-TR kültüründe formdan gelen "99.99" gibi
    // noktalı decimal değerlerin yanlış (9999 gibi) ayrıştırılmasını önler.
    options.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider());
});
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(trKultur, trKultur);
    options.SupportedCultures = new[] { trKultur };
    options.SupportedUICultures = new[] { trKultur };
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Veritabanını oluştur ve başlangıç (seed) verilerini yükle.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // EnsureCreated() yalnızca veritabanı hiç yoksa şemayı oluşturur; daha önceki bir
    // sürümden kalma, dosyaları kayıtlı mevcut bir AracSatis.db'de modele sonradan
    // eklenen tablolar/indeksler otomatik oluşmaz. Bu yüzden EF'nin o anki modelden
    // ürettiği TÜM "CREATE TABLE"/"CREATE INDEX" ifadeleri burada IF NOT EXISTS ile
    // tekrar çalıştırılır: modele sonradan eklenen her yeni tablo (ör. Memurlar,
    // AliciKayitlari, KomisyonOturumlari, AracModelleri) tek tek elle yazılmaya gerek
    // kalmadan eski bir AracSatis.db'de de otomatik oluşur; zaten var olan tablolara/
    // verilere dokunulmaz. SeedData.Initialize'daki EnsureCreated'ın (ki o da bu
    // güvenlik ağını tekrar çağırır, zararsız) ve seed sorgularının ÖNÜNDE çalışması
    // için en başta yapılır; aksi halde SeedData'nın eski tablolardan birini (henüz
    // oluşturulmamış yeni bir tablo oluşturulmadan önce) sorgulaması hataya düşebilir.
    db.Database.EnsureCreated();
    foreach (var ifadeHam in db.Database.GenerateCreateScript().Split(new[] { ";\r\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries))
    {
        var ifade = ifadeHam.Trim();
        if (ifade.Length == 0) continue;
        ifade = System.Text.RegularExpressions.Regex.Replace(ifade, @"^CREATE TABLE ", "CREATE TABLE IF NOT EXISTS ");
        ifade = System.Text.RegularExpressions.Regex.Replace(ifade, @"^CREATE (UNIQUE )?INDEX ", "CREATE $1INDEX IF NOT EXISTS ");
        try { db.Database.ExecuteSqlRaw(ifade + ";"); } catch { /* zaten varsa veya bağımlı tablo henüz yoksa göz ardı edilir */ }
    }

    SeedData.Initialize(db);

    // Tablo az önce oluşturulmuş olabileceğinden (yukarıdaki genel güvenlik ağıyla), başlangıç
    // model listesi burada - tablo garantiye alındıktan SONRA - doldurulur. Yalnızca tablo
    // tamamen boşsa (daha önce hiç çalıştırılmadıysa veya kullanıcı henüz hiç model
    // eklemediyse) bir kerelik doldurulur; kullanıcı sonradan sildiği modeller tekrar eklenmez.
    if (!db.AracModelleri.Any())
    {
        var modeller = new (string Marka, string[] Modeller)[]
        {
            ("Renault", new[] { "Clio", "Megane", "Symbol", "Fluence", "Taliant", "Duster", "Captur", "Kadjar", "Austral", "Talisman", "Laguna", "Kangoo" }),
            ("Fiat", new[] { "Egea", "Tipo", "Linea", "Albea", "Palio", "Doblo", "Fiorino", "Ducato", "Panda", "500" }),
            ("Ford", new[] { "Focus", "Fiesta", "Mondeo", "Kuga", "Puma", "Courier", "Transit", "Transit Custom", "Transit Connect", "Ranger" }),
            ("Volkswagen", new[] { "Polo", "Golf", "Passat", "Jetta", "Tiguan", "T-Roc", "T-Cross", "Taigo", "Touareg", "Caddy", "Transporter", "Crafter", "Amarok" }),
            ("Toyota", new[] { "Corolla", "Yaris", "Auris", "C-HR", "RAV4", "Hilux", "Avensis", "Camry", "Proace" }),
            ("Hyundai", new[] { "i10", "i20", "Accent", "Elantra", "Tucson", "Bayon", "Kona", "Santa Fe", "H-1", "Starex" }),
            ("Opel", new[] { "Corsa", "Astra", "Insignia", "Mokka", "Combo", "Vectra", "Vivaro" }),
            ("Peugeot", new[] { "208", "301", "308", "2008", "3008", "508", "Partner", "Boxer", "Expert" }),
            ("Citroen", new[] { "C3", "C-Elysee", "C4", "Berlingo", "Jumper", "Jumpy" }),
            ("Dacia", new[] { "Duster", "Sandero", "Logan", "Dokker", "Lodgy", "Spring" }),
            ("Honda", new[] { "Civic", "City", "Jazz", "CR-V", "HR-V" }),
            ("Nissan", new[] { "Micra", "Qashqai", "Juke", "X-Trail", "Primera", "Almera" }),
            ("Mercedes-Benz", new[] { "A Serisi", "C Serisi", "E Serisi", "Vito", "Sprinter", "Actros", "Atego", "Axor" }),
            ("BMW", new[] { "1 Serisi", "3 Serisi", "5 Serisi", "X1", "X3", "X5" }),
            ("Audi", new[] { "A3", "A4", "A6", "Q3", "Q5" }),
            ("Skoda", new[] { "Fabia", "Octavia", "Superb", "Kodiaq", "Karoq", "Rapid" }),
            ("Seat", new[] { "Ibiza", "Leon", "Arona", "Toledo" }),
            ("Kia", new[] { "Picanto", "Rio", "Ceed", "Sportage", "Stonic", "Carens" }),
            ("Chevrolet", new[] { "Aveo", "Cruze", "Lacetti", "Captiva", "Spark" }),
            ("Mazda", new[] { "Mazda2", "Mazda3", "CX-5" }),
            ("Suzuki", new[] { "Swift", "Vitara", "S-Cross", "Baleno" }),
            ("Mitsubishi", new[] { "Lancer", "Outlander", "L200", "ASX" }),
            ("Volvo", new[] { "S60", "S80", "XC60", "XC90", "FH", "FM" }),
            ("Land Rover", new[] { "Defender", "Discovery", "Range Rover", "Evoque" }),
            ("Jeep", new[] { "Renegade", "Compass", "Wrangler", "Grand Cherokee" }),
            ("Isuzu", new[] { "D-Max", "NPR", "NQR" }),
            ("Iveco", new[] { "Daily", "Eurocargo", "Stralis" }),
            ("MAN", new[] { "TGX", "TGS", "TGL", "Lion's City" }),
            ("Scania", new[] { "R Serisi", "P Serisi", "G Serisi" }),
            ("DAF", new[] { "XF", "CF", "LF" }),
            ("BMC", new[] { "Fatih", "Pro", "Yavuz", "Professional" }),
            ("Otokar", new[] { "Kent", "Sultan", "Doruk", "Territo" }),
            ("TEMSA", new[] { "Safir", "Avenue", "Maraton", "Prestij" }),
            ("Yamaha", new[] { "NMAX", "XMAX", "MT-07", "YBR125", "Crypton" }),
            ("Honda Motosiklet", new[] { "PCX", "CB500", "CBR", "Vario" }),
            ("TOGG", new[] { "T10X" }),
        };
        db.AracModelleri.AddRange(
            modeller.SelectMany(m => m.Modeller.Select(ad => new AracModeli { MarkaAdi = m.Marka, Ad = ad })));
        db.SaveChanges();
    }

    // Yukarıdaki ilk toplu liste girildikten sonra, markaların kendi resmi Türkiye
    // sitelerinden (Renault, Volkswagen, Fiat, Toyota, Hyundai, Opel, Peugeot, Citroën,
    // Dacia, Mercedes-Benz, BMW) doğrulanan ve o ilk listede eksik olan güncel modeller.
    // Yukarıdaki gibi "tablo boşsa" değil, her model tek tek "zaten yoksa" eklenir ki daha
    // önce doldurulmuş (ve kullanıcı tarafından düzenlenmiş) listelere de güvenle uygulanabilsin.
    {
        var yeniModeller = new (string Marka, string[] Modeller)[]
        {
            ("Renault", new[] { "Koleos", "Rafale", "Scenic E-Tech", "Renault 5 E-Tech", "Megane E-Tech", "Trafic", "Trafic Panelvan", "Trafic Combi", "Master", "Master Kamyonet" }),
            ("Volkswagen", new[] { "Tayron", "ID.4", "ID.7", "California", "Caravelle" }),
            ("Fiat", new[] { "600", "500e Armani", "Scudo" }),
            ("Toyota", new[] { "Yaris Cross", "Corolla Cross", "Land Cruiser Prado" }),
            ("Hyundai", new[] { "i30", "Staria", "Inster", "Ioniq 5", "Ioniq 6", "Ioniq 9" }),
            ("Opel", new[] { "Frontera", "Grandland", "Movano", "Zafira" }),
            ("Peugeot", new[] { "408", "5008", "Rifter" }),
            ("Citroen", new[] { "C3 Aircross", "C5 Aircross", "C4 X", "Ami" }),
            ("Dacia", new[] { "Sandero Stepway", "Jogger" }),
            ("Mercedes-Benz", new[] { "S Serisi", "GLA", "GLC", "GLE" }),
            ("BMW", new[] { "7 Serisi", "X6", "i4", "iX" }),
        };
        var mevcutlar = db.AracModelleri.Select(m => m.MarkaAdi + "|" + m.Ad).ToHashSet();
        var eklenecekler = yeniModeller
            .SelectMany(m => m.Modeller.Select(ad => new AracModeli { MarkaAdi = m.Marka, Ad = ad }))
            .Where(m => !mevcutlar.Contains(m.MarkaAdi + "|" + m.Ad))
            .ToList();
        if (eklenecekler.Count > 0)
        {
            db.AracModelleri.AddRange(eklenecekler);
            db.SaveChanges();
        }
    }

    // Çekici ücreti artık (otopark tarifesi gibi) araç kategorisine göre 4 ayrı sütunda
    // tutuluyor (eski tek "CekiciUcretiSabit" sütunu yerine). SQLite'ta "ADD COLUMN IF NOT
    // EXISTS" olmadığından her biri ayrı ayrı denenip zaten varsa oluşan hata yutulur.
    foreach (var sutun in new[] { "KucukAracCekiciUcreti", "MinibusKamyonetCekiciUcreti", "BuyukAracCekiciUcreti", "MotorCekiciUcreti" })
    {
        try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""GenelAyarlar"" ADD COLUMN """ + sutun + @""" decimal(18,2) NOT NULL DEFAULT 0;"); }
        catch { /* sütun zaten varsa göz ardı edilir */ }
    }

    // Yukarıdaki ALTER TABLE ile sütunlar 0 TL olarak eklenmiş olabilir (henüz hiç
    // "Güncellemeler" ekranından değiştirilmemiş kayıtlar için); bu durumda araç
    // kategorisine göre makul varsayılan çekici ücretleri otomatik doldurulur: büyük
    // araç (kamyon/tır) 5.000 TL, diğer tüm kategoriler (küçük araç, minibüs/kamyonet,
    // motor) 2.000 TL. Kullanıcı Güncellemeler ekranından kendi değerini girdiyse (0 veya
    // daha önceki geçici varsayılanlardan biri değilse) dokunulmaz; bu yüzden her
    // başlangıçta çalıştırmak güvenlidir.
    try
    {
        db.Database.ExecuteSqlRaw(@"UPDATE ""GenelAyarlar"" SET
            ""KucukAracCekiciUcreti"" = CASE WHEN ""KucukAracCekiciUcreti"" = 0 THEN 2000 ELSE ""KucukAracCekiciUcreti"" END,
            ""MinibusKamyonetCekiciUcreti"" = CASE WHEN ""MinibusKamyonetCekiciUcreti"" IN (0, 3000) THEN 2000 ELSE ""MinibusKamyonetCekiciUcreti"" END,
            ""BuyukAracCekiciUcreti"" = CASE WHEN ""BuyukAracCekiciUcreti"" = 0 THEN 5000 ELSE ""BuyukAracCekiciUcreti"" END,
            ""MotorCekiciUcreti"" = CASE WHEN ""MotorCekiciUcreti"" IN (0, 1000) THEN 2000 ELSE ""MotorCekiciUcreti"" END;");
    }
    catch { /* tablo/sütunlar bir şekilde yoksa sessizce geçilir */ }

    // "Tır" (büyük araç kategorisi), mevcut veritabanlarının Araç Cinsleri listesinde
    // henüz olmayabilir (yalnızca ilk kurulumda seed ile eklenir); burada adı geçmiyorsa
    // eklenir. Kullanıcı bunu "Güncellemeler" ekranından daha sonra silebilir/değiştirebilir.
    if (!db.AracCinsleri.Any(c => c.Ad == "Tır"))
    {
        db.AracCinsleri.Add(new AracCinsi { Ad = "Tır", OtoparkKategorisi = OtoparkAracKategorisi.BuyukArac });
        db.SaveChanges();
    }
}

// Veri güvenliği: uygulama her başlatıldığında veritabanının otomatik bir yedeğini al.
try
{
    var dbYolu = Path.Combine(Directory.GetCurrentDirectory(), "AracSatis.db");
    if (File.Exists(dbYolu))
    {
        var yedekKlasoru = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
        Directory.CreateDirectory(yedekKlasoru);
        var yedekYolu = Path.Combine(yedekKlasoru, $"AracSatis_{DateTime.Now:yyyyMMdd_HHmmss}.db");
        File.Copy(dbYolu, yedekYolu, overwrite: true);

        // Sadece son 20 yedeği tut, eskileri temizle.
        var yedekler = Directory.GetFiles(yedekKlasoru, "AracSatis_*.db")
            .OrderByDescending(f => f)
            .Skip(20);
        foreach (var eski in yedekler) File.Delete(eski);
    }
}
catch
{
    // Yedekleme başarısız olsa bile uygulamanın açılmasını engelleme.
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Hata");
    app.UseHsts();
}

app.UseRequestLocalization();
app.UseHttpsRedirection();
app.UseStaticFiles();

// İhaleyi kazanan alıcı, satış tarihinden sonraki 3 iş günü içinde idareye hiç
// gelmediyse (Alıcı Ödeme Bilgileri hiç girilmediyse), her istekte otomatik olarak
// "Alıcı Çıkmadı"ya çevrilip dosya bir sonraki satış aşamasına alınır.
app.Use(async (context, next) =>
{
    var db = context.RequestServices.GetRequiredService<AppDbContext>();
    await OtomatikSurecIsleyici.AliciGelmeyenSatislariGuncelleAsync(db);
    await next();
});

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
