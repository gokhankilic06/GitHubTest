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

    // Araç cinsi/marka/model, vergi dairesi, otopark ve renk referans listeleri artık
    // Data/ReferansVerileri/*.json dosyalarından okunup (kodda sabit liste olarak değil)
    // burada, tablolar garantiye alındıktan SONRA yükleniyor - bkz. SeedData.Initialize.
    SeedData.Initialize(db);

    // Eski "Diğer" (düz metin, tek başına seçilen) renk satırı siliniyor çünkü artık
    // Araç Tipi'ndeki gibi gerçek bir "Diğer (elle yaz)" seçeneği var; ikisi birden
    // listede görünmesin diye. (Yeni kurulumlarda zaten JSON listesinde yok.)
    {
        var eskiDigerKaydi = db.AracRenkleri.FirstOrDefault(r => r.Ad == "Diğer");
        if (eskiDigerKaydi != null)
        {
            db.AracRenkleri.Remove(eskiDigerKaydi);
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

    // "Model Yılı Üst Sınırı" ayarı (Kayıt İşlemleri/Dosya Düzenle ekranındaki "Model
    // Yılı" listesinin üst sınırı) sonradan eklendi; eski veritabanlarında sütun yoksa
    // eklenir ve 0 ise (henüz hiç "Güncellemeler" ekranından değiştirilmemişse) makul bir
    // varsayılana (2027) ayarlanır. Kullanıcı kendi değerini girdiyse dokunulmaz.
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""GenelAyarlar"" ADD COLUMN ""MaksimumModelYili"" INTEGER NOT NULL DEFAULT 0;"); }
    catch { /* sütun zaten varsa göz ardı edilir */ }
    try { db.Database.ExecuteSqlRaw(@"UPDATE ""GenelAyarlar"" SET ""MaksimumModelYili"" = 2027 WHERE ""MaksimumModelYili"" = 0;"); }
    catch { /* tablo/sütun bir şekilde yoksa sessizce geçilir */ }

    // "Damga Vergisi Oranı" artık dosya bazında saklanıp düzenlenebiliyor (eskiden hep
    // sabit binde 5,69 olarak hesaplanıyordu); eski veritabanlarında sütun yoksa eklenir
    // ve mevcut tüm dosyalara o zamana kadar geçerli olan binde 5,69 değeri atanır.
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Dosyalar"" ADD COLUMN ""DamgaVergisiOrani"" decimal(18,2) NOT NULL DEFAULT 5.69;"); }
    catch { /* sütun zaten varsa göz ardı edilir */ }
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
