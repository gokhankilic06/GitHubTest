using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using VerasetRandevu;

var kok = Ayarlar.KokBul();
var ayarlar = Ayarlar.Yukle(Path.Combine(kok, "config.json"));
var web = Path.Combine(kok, "web");
var takvim = new Takvim(ayarlar);
var dbYolu = Environment.GetEnvironmentVariable("DB_PATH") is { Length: > 0 } d ? d : Path.Combine(kok, "data", "randevu.db");
var depo = new Depo(dbYolu, Path.Combine(kok, "schema.sql"));

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = kok,
    WebRootPath = Path.Combine(web, "public"),
});
var port = Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } p ? p : "5100";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 20 * 1024);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All));

var app = builder.Build();

// SIMDI ortam değişkeni (ISO tarih) test amaçlı sabit bir "şu an" vermeyi sağlar.
static DateTimeOffset Simdi() =>
    Environment.GetEnvironmentVariable("SIMDI") is { Length: > 0 } s
        ? DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
        : DateTimeOffset.UtcNow;

static IResult Hata(int durum, string mesaj, Dictionary<string, string>? hatalar = null) =>
    hatalar == null
        ? Results.Json(new { hata = mesaj }, statusCode: durum)
        : Results.Json(new { hata = mesaj, hatalar }, statusCode: durum);

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.XContentTypeOptions = "nosniff";
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/ayarlar", () => Results.Json(new
{
    vergiDairesi = ayarlar.VergiDairesi,
    ilceler = ayarlar.Ilceler,
    yeniGunAcilisSaati = ayarlar.YeniGunAcilisSaati,
    belge = new
    {
        ad = ayarlar.Belge.Ad,
        url = "/belgeler/" + Uri.EscapeDataString(ayarlar.Belge.Dosya),
        mevcut = File.Exists(Path.Combine(web, "public", "belgeler", ayarlar.Belge.Dosya)),
    },
}));

app.MapGet("/api/gunler", () =>
{
    var p = takvim.Pencere(Simdi());
    var doluluk = depo.GunlukDoluluk(p.Ilk, p.Son);
    var gunler = new Dictionary<string, string>();
    for (var t = p.Ilk; string.CompareOrdinal(t, p.Son) <= 0; t = Takvim.GunEkle(t, 1))
    {
        var kapasite = takvim.GunSaatleri(t).Count * ayarlar.SlotKapasitesi;
        gunler[t] = kapasite == 0 ? "kapali" : doluluk.GetValueOrDefault(t) >= kapasite ? "dolu" : "uygun";
    }
    return Results.Json(new { bugun = p.Bugun, ilk = p.Ilk, son = p.Son, gunler });
});

app.MapGet("/api/saatler", (string? tarih) =>
{
    if (!takvim.GunAcikMi(tarih, takvim.Pencere(Simdi())))
    {
        return Hata(400, "Seçilen gün için randevu alınamaz.");
    }
    var doluluk = depo.SaatlikDoluluk(tarih!);
    var saatler = takvim.GunSaatleri(tarih!)
        .Select(s => new { saat = s, uygun = doluluk.GetValueOrDefault(s) < ayarlar.SlotKapasitesi });
    return Results.Json(new { tarih, saatler });
});

// İstek gövdesini JSON olarak okur; geçersizse null döner.
static async Task<JsonElement?> JsonOku(HttpContext ctx)
{
    try
    {
        using var belge = await JsonDocument.ParseAsync(ctx.Request.Body);
        return belge.RootElement.Clone();
    }
    catch (Exception e) when (e is JsonException or BadHttpRequestException)
    {
        return null;
    }
}

app.MapPost("/api/randevular", async (HttpContext ctx) =>
{
    if (await JsonOku(ctx) is not { } govde) return Hata(400, "Geçersiz istek.");

    var p = takvim.Pencere(Simdi());
    var buYil = int.Parse(p.Bugun[..4], CultureInfo.InvariantCulture);
    var (veri, hatalar) = Dogrulama.RandevuDogrula(govde, ayarlar.Ilceler, buYil);

    if (!takvim.GunAcikMi(veri.Tarih, p)) hatalar["tarih"] = "Seçilen gün için randevu alınamaz.";
    else if (!takvim.GunSaatleri(veri.Tarih).Contains(veri.Saat)) hatalar["saat"] = "Geçerli bir randevu saati seçiniz.";

    if (hatalar.Count > 0) return Hata(400, "Lütfen hatalı alanları düzeltiniz.", hatalar);

    var (randevu, cakisma) = depo.Ekle(veri, p.Bugun, ayarlar.SlotKapasitesi);
    if (cakisma != null)
    {
        return Hata(409, cakisma.Mesaj, new Dictionary<string, string> { [cakisma.Alan] = cakisma.Mesaj });
    }
    return Results.Json(new { randevu }, statusCode: 201);
});

// Vatandaşın randevu numarası ve kendi T.C. numarasıyla randevusunu bulması.
(Randevu? randevu, IResult? hata) VatandasRandevusu(JsonElement g)
{
    static string Alan(JsonElement g, string ad) =>
        g.ValueKind == JsonValueKind.Object && g.TryGetProperty(ad, out var e) && e.ValueKind == JsonValueKind.String
            ? e.GetString()!.Trim()
            : "";

    var randevuNo = Alan(g, "randevuNo").ToUpperInvariant();
    var basvuranTc = Alan(g, "basvuranTc");
    if (randevuNo.Length != 8 || !randevuNo.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9') ||
        basvuranTc.Length != 11 || !basvuranTc.All(c => c is >= '0' and <= '9'))
    {
        return (null, Hata(400, "Randevu numarası ve T.C. Kimlik Numarası eksik veya hatalı."));
    }
    var r = depo.NumaraIleGetir(randevuNo);
    if (r == null || r.BasvuranTc != basvuranTc)
    {
        return (null, Hata(404, "Girdiğiniz bilgilerle eşleşen bir randevu bulunamadı."));
    }
    return (r, null);
}

// Vatandaşa gösterilen randevu özeti (T.C. ve telefon numaraları gösterilmez).
object VatandasOzeti(Randevu r, string bugun) => new
{
    randevuNo = r.RandevuNo,
    vergiDairesi = ayarlar.VergiDairesi,
    ilce = r.Ilce,
    tarih = r.Tarih,
    saat = r.Saat,
    vefatEdenAd = r.VefatEdenAd,
    vefatEdenSoyad = r.VefatEdenSoyad,
    basvuranAd = r.BasvuranAd,
    basvuranSoyad = r.BasvuranSoyad,
    durum = r.Durum,
    iptalEdilebilir = r.Durum == "aktif" && string.CompareOrdinal(r.Tarih, bugun) >= 0,
};

app.MapPost("/api/randevu-sorgula", async (HttpContext ctx) =>
{
    if (await JsonOku(ctx) is not { } govde) return Hata(400, "Geçersiz istek.");
    var (r, hata) = VatandasRandevusu(govde);
    if (hata != null) return hata;
    return Results.Json(new { randevu = VatandasOzeti(r!, takvim.Pencere(Simdi()).Bugun) });
});

app.MapPost("/api/randevu-iptal", async (HttpContext ctx) =>
{
    if (await JsonOku(ctx) is not { } govde) return Hata(400, "Geçersiz istek.");
    var (r, hata) = VatandasRandevusu(govde);
    if (hata != null) return hata;
    var bugun = takvim.Pencere(Simdi()).Bugun;
    if (r!.Durum != "aktif") return Hata(409, "Bu randevu zaten iptal edilmiş.");
    if (string.CompareOrdinal(r.Tarih, bugun) < 0) return Hata(409, "Tarihi geçmiş randevular iptal edilemez.");
    if (!depo.IptalEt(r.Id)) return Hata(409, "Bu randevu zaten iptal edilmiş.");
    return Results.Json(new { randevu = VatandasOzeti(depo.Getir(r.Id)!, bugun) });
});

// Personel paneli için HTTP Basic kimlik doğrulaması.
var yonetim = app.MapGroup("").AddEndpointFilter(async (ctx, next) =>
{
    var sifre = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
    if (string.IsNullOrEmpty(sifre))
    {
        return Hata(503, "Yönetim paneli kapalı: ADMIN_PASSWORD ortam değişkeni tanımlanmamış.");
    }
    var kullanici = Environment.GetEnvironmentVariable("ADMIN_USER") is { Length: > 0 } u ? u : "yonetici";

    static bool EsitMi(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    var baslik = ctx.HttpContext.Request.Headers.Authorization.ToString();
    if (baslik.StartsWith("Basic ", StringComparison.Ordinal))
    {
        try
        {
            var cozulen = Encoding.UTF8.GetString(Convert.FromBase64String(baslik[6..].Trim()));
            var ayrac = cozulen.IndexOf(':');
            if (ayrac >= 0 && EsitMi(cozulen[..ayrac], kullanici) && EsitMi(cozulen[(ayrac + 1)..], sifre))
            {
                return await next(ctx);
            }
        }
        catch (FormatException)
        {
        }
    }
    ctx.HttpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Randevu Yonetimi\", charset=\"UTF-8\"";
    return Hata(401, "Yetkisiz erişim.");
});

yonetim.MapGet("/yonetim", () => Results.File(Path.Combine(web, "yonetim.html"), "text/html; charset=utf-8"));

yonetim.MapGet("/api/yonetim/randevular", (string? tarih) =>
{
    if (!string.IsNullOrEmpty(tarih) && !Takvim.GecerliTarihMi(tarih)) return Hata(400, "Geçersiz tarih.");
    return Results.Json(new { randevular = depo.Listele(tarih, takvim.Pencere(Simdi()).Bugun) });
});

yonetim.MapPost("/api/yonetim/randevular/{id:long}/iptal", (long id) =>
    depo.IptalEt(id) ? Results.Json(new { tamam = true }) : Hata(404, "Aktif randevu bulunamadı."));

app.Map("/api/{**yol}", () => Hata(404, "Bulunamadı."));

Console.WriteLine($"Veraset randevu sistemi (C#) http://localhost:{port} adresinde çalışıyor.");
app.Run();
