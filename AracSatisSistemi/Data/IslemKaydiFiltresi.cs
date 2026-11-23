using Microsoft.AspNetCore.Mvc.Filters;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Data
{
    // Her POST isteğini (sayfa görüntüleme değil, kayıt ekleme/düzenleme/silme gibi
    // durum değiştiren işlemleri) IslemKayitlari tablosuna yazan genel (global) action
    // filter - Kullanıcılar (istatistik) sayfasındaki "hangi işlemleri yaptı" listesi
    // için. Program.cs'te tüm controller'lara otomatik uygulanır (bkz.
    // AddControllersWithViews). Giriş/Kayıt Ol/Çıkış ayrıca GirisKaydi ile takip
    // edildiğinden burada tekrar loglanmaz.
    public class IslemKaydiFiltresi : IAsyncActionFilter
    {
        private readonly AppDbContext _db;
        public IslemKaydiFiltresi(AppDbContext db) => _db = db;

        private static readonly HashSet<string> HaricTutulanlar = new(StringComparer.OrdinalIgnoreCase)
        {
            "Hesap/Giris", "Hesap/KayitOl", "Hesap/Cikis"
        };

        // Controller/Action adlarından daha okunur, Türkçe bir işlem açıklaması üretir;
        // eşleşme yoksa ham "Controller/Action" değerine düşer.
        private static readonly Dictionary<string, string> Aciklamalar = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Dosya/Create"] = "Yeni dosya kaydı oluşturdu",
            ["Dosya/Edit"] = "Dosya bilgilerini düzenledi",
            ["Satis/SonucGir"] = "Satış sonucu girdi",
            ["Satis/Edit"] = "Satış bilgilerini düzenledi",
            ["AliciOdeme/Gir"] = "Alıcı ödeme bilgilerini kaydetti",
            ["IptalIade/Islem"] = "Satış iptal/iade işlemi yaptı",
            ["IptalIade/Kapat"] = "Dosya kapattı",
            ["IptalIade/AliciyaTerk"] = "Alıcıya terk işlemi kaydetti",
            ["IptalIade/AliciSuresindeGitmedi"] = "\"Alıcı süresinde gitmedi\" işaretledi",
            ["Komisyon/Create"] = "Komisyon oturumu ekledi",
            ["Komisyon/Sil"] = "Komisyon oturumu sildi",
            ["Otopark/Ekle"] = "Otopark ekledi",
            ["Otopark/Duzenle"] = "Otopark bilgilerini düzenledi",
            ["Otopark/Sil"] = "Otopark sildi",
            ["SatisTakvimi/Ekle"] = "Satış takvimine tarih ekledi",
            ["SatisTakvimi/TopluEkle"] = "Satış takvimine toplu tarih ekledi",
            ["SatisTakvimi/ExcelYukle"] = "Satış takvimine Excel'den tarih yükledi",
            ["SatisTakvimi/Sil"] = "Satış takviminden tarih sildi",
            ["Hesap/SifreDegistir"] = "Kendi şifresini değiştirdi",
            ["Ayarlar/KullaniciEkle"] = "Yeni kullanıcı ekledi",
            ["Ayarlar/KullaniciSil"] = "Kullanıcı sildi",
            ["Ayarlar/KullaniciSifreSifirla"] = "Bir kullanıcının şifresini sıfırladı",
            ["Ayarlar/DavetKoduGuncelle"] = "Davet kodunu güncelledi",
        };

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next();

            if (!string.Equals(context.HttpContext.Request.Method, "POST", StringComparison.OrdinalIgnoreCase)) return;
            if (context.HttpContext.User.Identity?.IsAuthenticated != true) return;

            var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
            var action = context.RouteData.Values["action"]?.ToString() ?? "";
            var anahtar = $"{controller}/{action}";
            if (HaricTutulanlar.Contains(anahtar)) return;

            var detayParcalari = context.ActionArguments
                .Where(kv => kv.Value != null && (kv.Value is int or string || kv.Value.GetType().IsPrimitive))
                .Select(kv => $"{kv.Key}={kv.Value}")
                .Take(5);

            _db.IslemKayitlari.Add(new IslemKaydi
            {
                KullaniciAdi = context.HttpContext.User.Identity!.Name ?? "?",
                Zaman = DateTime.Now,
                Controller = controller,
                Action = action,
                Detay = Aciklamalar.TryGetValue(anahtar, out var aciklama)
                    ? aciklama
                    : string.Join(", ", detayParcalari)
            });
            await _db.SaveChangesAsync();
        }
    }
}
