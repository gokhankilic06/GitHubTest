using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;

// Uygulama tamamen Türkçe (tarih/sayı biçimleri: 300.000,00 gibi) olduğundan varsayılan
// kültür sunucunun işletim sistemi ayarından bağımsız olarak sabit tr-TR yapılır.
var trKultur = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = trKultur;
CultureInfo.DefaultThreadCurrentUICulture = trKultur;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
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
