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
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
