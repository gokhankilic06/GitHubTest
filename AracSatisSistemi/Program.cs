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
    SeedData.Initialize(db);

    // EnsureCreated() yalnızca veritabanı hiç yoksa şemayı oluşturur; daha önceki bir
    // sürümden kalma, dosyaları kayıtlı mevcut bir AracSatis.db'de modele sonradan
    // eklenen tablolar otomatik oluşmaz. Bu yüzden yeni eklenen tablo burada elle
    // (varsa dokunmadan) garanti altına alınır ki mevcut kayıtlar kaybolmadan yeni
    // özellik çalışsın.
    db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""SatisTarihleri"" (
        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SatisTarihleri"" PRIMARY KEY AUTOINCREMENT,
        ""Tarih"" TEXT NOT NULL
    );");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_SatisTarihleri_Tarih"" ON ""SatisTarihleri"" (""Tarih"");");

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
