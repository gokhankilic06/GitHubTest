using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    // Her POST işlemini (kayıt ekleme/düzenleme/silme vb.) IslemKayitlari'na yazar -
    // bkz. Kullanıcılar (istatistik) sayfası.
    options.Filters.Add<AracSatisSistemi.Data.IslemKaydiFiltresi>();
});
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(trKultur, trKultur);
    options.SupportedCultures = new[] { trKultur };
    options.SupportedUICultures = new[] { trKultur };
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Giriş güvenliği: çerez tabanlı kimlik doğrulama. "Güncellemeler > Kullanıcılar"
// ekranından eklenen kullanıcı adı/şifre ile giriş yapılır; şifreler PasswordHasher
// (PBKDF2) ile hash'lenerek saklanır, düz metin hiçbir yerde tutulmaz.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Hesap/Giris";
        options.AccessDeniedPath = "/Hesap/Giris";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    });
// Günlük (son 60 gün) ve haftalık (her Cuma, hiç silinmeyen) otomatik veritabanı
// yedeklemesi - bkz. Services/YedeklemeServisi.cs.
builder.Services.AddHostedService<AracSatisSistemi.Services.YedeklemeServisi>();

builder.Services.AddAuthorization(options =>
{
    // Varsayılan olarak TÜM ekranlar giriş gerektirir ([AllowAnonymous] ile işaretlenen
    // Hesap/Giris hariç); her controller'a tek tek [Authorize] eklemeye gerek kalmaz.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// Veritabanını oluştur ve başlangıç (seed) verilerini yükle.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // EnsureCreated() yalnızca veritabanı hiç yoksa şemayı oluşturur; daha önceki bir
    // sürümden kalma, dosyaları kayıtlı mevcut bir AracSatis.db'de modele sonradan
    // eklenen tablolar/indeksler otomatik oluşmaz. Bu yüzden EF'nin o anki modelden
    // ürettiği TÜM "CREATE TABLE"/"CREATE INDEX" ifadeleri burada IF NOT EXISTS ile
    // tekrar çalıştırılır: modele sonradan eklenen her yeni tablo (ör. AliciKayitlari,
    // KomisyonOturumlari, AracModelleri) tek tek elle yazılmaya gerek
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
        // EF1003: "ifade" kullanıcı girdisi değil, doğrudan EF'nin kendi modelinden
        // ürettiği GenerateCreateScript() çıktısıdır; SQL enjeksiyon riski yoktur.
#pragma warning disable EF1003
        try { db.Database.ExecuteSqlRaw(ifade + ";"); } catch { /* zaten varsa veya bağımlı tablo henüz yoksa göz ardı edilir */ }
#pragma warning restore EF1003
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
    // EF1003: "sutun" kullanıcı girdisi değil, yukarıdaki sabit dizideki bilinen sütun
    // adlarından biridir; SQL enjeksiyon riski yoktur.
#pragma warning disable EF1003
    foreach (var sutun in new[] { "KucukAracCekiciUcreti", "MinibusKamyonetCekiciUcreti", "BuyukAracCekiciUcreti", "MotorCekiciUcreti" })
    {
        try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""GenelAyarlar"" ADD COLUMN """ + sutun + @""" decimal(18,2) NOT NULL DEFAULT 0;"); }
        catch { /* sütun zaten varsa göz ardı edilir */ }
    }
#pragma warning restore EF1003

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

    // "Kayıt Ol Davet Kodu" ayarı sonradan eklendi; eski veritabanlarında sütun yoksa eklenir.
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""GenelAyarlar"" ADD COLUMN ""DavetKodu"" TEXT NULL;"); }
    catch { /* sütun zaten varsa göz ardı edilir */ }

    // "Damga Vergisi Oranı" artık dosya bazında saklanıp düzenlenebiliyor (eskiden hep
    // sabit binde 5,69 olarak hesaplanıyordu); eski veritabanlarında sütun yoksa eklenir
    // ve mevcut tüm dosyalara o zamana kadar geçerli olan binde 5,69 değeri atanır.
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Dosyalar"" ADD COLUMN ""DamgaVergisiOrani"" decimal(18,2) NOT NULL DEFAULT 5.69;"); }
    catch { /* sütun zaten varsa göz ardı edilir */ }

    // Otopark kayıtlarına Adres/Yetkili-Çalışanı/Telefon alanları sonradan eklendi; eski
    // veritabanlarında sütun yoksa eklenir.
    foreach (var sutun in new[] { "Adres", "YetkiliCalisani", "Telefon" })
    {
        try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Otoparklar"" ADD COLUMN """ + sutun + @""" TEXT NULL;"); }
        catch { /* sütun zaten varsa göz ardı edilir */ }
    }

    // İlk kurulumda hiç kullanıcı yoksa, giriş yapılabilsin diye varsayılan bir "admin"
    // kullanıcısı oluşturulur. Güncellemeler > Kullanıcılar ekranından şifre değiştirilmeli
    // ve gerekirse diğer kullanıcılar eklenmelidir.
    if (!db.Kullanicilar.Any())
    {
        var hasher = new PasswordHasher<Kullanici>();
        var admin = new Kullanici { KullaniciAdi = "admin", AdSoyad = "Yönetici", Aktif = true, Admin = true };
        admin.SifreHash = hasher.HashPassword(admin, "Admin123!");
        db.Kullanicilar.Add(admin);
        db.SaveChanges();
    }

    // Kullanıcılara Admin yetkisi sonradan eklendi; eski veritabanlarında sütun yoksa
    // eklenir. Sistemin kurulu olduğu "admin" hesabı her zaman Admin kalır - aksi halde
    // bu güncellemeden sonra ilk açılışta hiç admin kalmayabilir.
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Kullanicilar"" ADD COLUMN ""Admin"" INTEGER NOT NULL DEFAULT 0;"); }
    catch { /* sütun zaten varsa göz ardı edilir */ }
    try { db.Database.ExecuteSqlRaw(@"UPDATE ""Kullanicilar"" SET ""Admin"" = 1 WHERE ""KullaniciAdi"" = 'admin';"); }
    catch { /* tablo/sütun bir şekilde yoksa sessizce geçilir */ }

    // Alıcıya Terk'e, hızlı/tekil kayıt ekranı için "Terk Yazı Tarihi/Sayısı" sonradan eklendi.
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""IptalIadeler"" ADD COLUMN ""TerkYaziTarihi"" TEXT NULL;"); }
    catch { /* sütun zaten varsa göz ardı edilir */ }
    try { db.Database.ExecuteSqlRaw(@"ALTER TABLE ""IptalIadeler"" ADD COLUMN ""TerkYaziSayisi"" TEXT NULL;"); }
    catch { /* sütun zaten varsa göz ardı edilir */ }

    // Daha önceki sürümlerde Komisyon Bilgileri (Ayarlar) ilk kurulumda "Üye 1 (güncelleyin)"
    // gibi yer tutucu metinlerle oluşturuluyordu; bu metinler kalıcı veri olarak kaydedilmiş
    // olabileceğinden, hâlâ tam olarak bu haliyle duran satırlar burada boşa çevrilir.
    try
    {
        db.Database.ExecuteSqlRaw(@"UPDATE ""KomisyonAyarlari"" SET ""Baskan"" = '' WHERE ""Baskan"" = 'Komisyon Başkanı (güncelleyin)';");
        db.Database.ExecuteSqlRaw(@"UPDATE ""KomisyonAyarlari"" SET ""Uye1"" = '' WHERE ""Uye1"" = 'Üye 1 (güncelleyin)';");
        db.Database.ExecuteSqlRaw(@"UPDATE ""KomisyonAyarlari"" SET ""Uye2"" = '' WHERE ""Uye2"" = 'Üye 2 (güncelleyin)';");
        db.Database.ExecuteSqlRaw(@"UPDATE ""KomisyonAyarlari"" SET ""Uye3"" = '' WHERE ""Uye3"" = 'Üye 3 (güncelleyin)';");
    }
    catch { /* tablo/sütun bir şekilde yoksa sessizce geçilir */ }

    // Komisyon Oturumu formu, Başkan/Üye alanlarını yukarıdaki varsayılan Komisyon
    // Bilgileri'yle önceden dolduruyor; bu yüzden bir kullanıcı değiştirmeden kaydettiyse
    // aynı yer tutucu metin gerçek bir oturum kaydına (ör. İhale Tutanağı'nda görünen) da
    // işlemiş olabilir - o satırlar da burada boşa çevrilir.
    try
    {
        db.Database.ExecuteSqlRaw(@"UPDATE ""KomisyonOturumlari"" SET ""Baskan"" = '' WHERE ""Baskan"" = 'Komisyon Başkanı (güncelleyin)';");
        db.Database.ExecuteSqlRaw(@"UPDATE ""KomisyonOturumlari"" SET ""Uye1"" = '' WHERE ""Uye1"" = 'Üye 1 (güncelleyin)';");
        db.Database.ExecuteSqlRaw(@"UPDATE ""KomisyonOturumlari"" SET ""Uye2"" = '' WHERE ""Uye2"" = 'Üye 2 (güncelleyin)';");
        db.Database.ExecuteSqlRaw(@"UPDATE ""KomisyonOturumlari"" SET ""Uye3"" = '' WHERE ""Uye3"" = 'Üye 3 (güncelleyin)';");
    }
    catch { /* tablo/sütun bir şekilde yoksa sessizce geçilir */ }

    // Kayıt İşlemleri (Dosya oluşturma) de aynı varsayılan Komisyon Bilgileri'ni dosyanın
    // kendi Komisyon alanlarına kopyalıyor (bkz. DosyaController.Create GET, İhale Tutanağı
    // belgesinde dosya.KomisyonBaskani/Uye1/Uye2/Uye3 geri düşüş olarak kullanılıyor) - bu
    // yüzden aynı yer tutucu metin dosyalara da işlemiş olabilir, burada temizlenir.
    try
    {
        db.Database.ExecuteSqlRaw(@"UPDATE ""Dosyalar"" SET ""KomisyonBaskani"" = NULL WHERE ""KomisyonBaskani"" = 'Komisyon Başkanı (güncelleyin)';");
        db.Database.ExecuteSqlRaw(@"UPDATE ""Dosyalar"" SET ""KomisyonUye1"" = NULL WHERE ""KomisyonUye1"" = 'Üye 1 (güncelleyin)';");
        db.Database.ExecuteSqlRaw(@"UPDATE ""Dosyalar"" SET ""KomisyonUye2"" = NULL WHERE ""KomisyonUye2"" = 'Üye 2 (güncelleyin)';");
        db.Database.ExecuteSqlRaw(@"UPDATE ""Dosyalar"" SET ""KomisyonUye3"" = NULL WHERE ""KomisyonUye3"" = 'Üye 3 (güncelleyin)';");
    }
    catch { /* tablo/sütun bir şekilde yoksa sessizce geçilir */ }
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
// "Alıcı İşlem Yapmadı"ya çevrilip dosya bir sonraki satış aşamasına alınır.
app.Use(async (context, next) =>
{
    var db = context.RequestServices.GetRequiredService<AppDbContext>();
    await OtomatikSurecIsleyici.AliciGelmeyenSatislariGuncelleAsync(db);
    await next();
});

// Giriş yapmış kullanıcının oturumuna ait GirisKaydi'nın "son aktivite" zamanını
// günceller - bkz. Kullanıcılar (istatistik) sayfası, Data/KullaniciAktiviteIzleyici.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var girisKaydiIdClaim = context.User.FindFirst("GirisKaydiId")?.Value;
        if (int.TryParse(girisKaydiIdClaim, out var girisKaydiId))
        {
            var db = context.RequestServices.GetRequiredService<AppDbContext>();
            await KullaniciAktiviteIzleyici.GuncelleAsync(db, girisKaydiId);
        }
    }
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
