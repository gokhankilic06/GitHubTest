using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;

namespace AracSatisSistemi.Controllers
{
    // KULLANICILAR (İSTATİSTİK) ekranı: sadece admin erişebilir. Her kullanıcının ne
    // zaman giriş yaptığı, oturumlarının ne kadar sürdüğü ve (sayfa görüntüleme hariç)
    // hangi işlemleri yaptığı burada görüntülenir - bkz. GirisKaydi, IslemKaydi,
    // Data/IslemKaydiFiltresi, Data/KullaniciAktiviteIzleyici.
    [Authorize(Roles = "Admin")]
    public class KullanicilarController : Controller
    {
        private readonly AppDbContext _db;
        public KullanicilarController(AppDbContext db) => _db = db;

        public class KullaniciOzet
        {
            public int Id { get; set; }
            public string KullaniciAdi { get; set; } = string.Empty;
            public string? AdSoyad { get; set; }
            public bool Admin { get; set; }
            public bool Aktif { get; set; }
            public int GirisSayisi { get; set; }
            public DateTime? SonGiris { get; set; }
            public bool SuAnOnlineMi { get; set; }
            public TimeSpan ToplamSure { get; set; }
            public int IslemSayisi { get; set; }
        }

        public async Task<IActionResult> Index()
        {
            var kullanicilar = await _db.Kullanicilar.OrderBy(k => k.KullaniciAdi).ToListAsync();
            var tumGirisler = await _db.GirisKayitlari.ToListAsync();
            var islemSayilari = await _db.IslemKayitlari
                .GroupBy(i => i.KullaniciAdi)
                .Select(g => new { g.Key, Adet = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Adet);

            var ozetler = kullanicilar.Select(k =>
            {
                var girisleri = tumGirisler.Where(g => g.KullaniciId == k.Id).ToList();
                return new KullaniciOzet
                {
                    Id = k.Id,
                    KullaniciAdi = k.KullaniciAdi,
                    AdSoyad = k.AdSoyad,
                    Admin = k.Admin,
                    Aktif = k.Aktif,
                    GirisSayisi = girisleri.Count,
                    SonGiris = girisleri.Select(g => (DateTime?)g.GirisZamani).Max(),
                    SuAnOnlineMi = girisleri.Any(g => g.CikisZamani == null && (DateTime.Now - g.SonAktiviteZamani) < TimeSpan.FromMinutes(15)),
                    ToplamSure = girisleri.Aggregate(TimeSpan.Zero, (toplam, g) => toplam + g.Sure),
                    IslemSayisi = islemSayilari.GetValueOrDefault(k.KullaniciAdi)
                };
            }).ToList();

            return View(ozetler);
        }

        public async Task<IActionResult> Detay(int id)
        {
            var kullanici = await _db.Kullanicilar.FindAsync(id);
            if (kullanici == null) return NotFound();

            ViewBag.Kullanici = kullanici;
            var girisleri = await _db.GirisKayitlari
                .Where(g => g.KullaniciId == id)
                .OrderByDescending(g => g.GirisZamani)
                .Take(200)
                .ToListAsync();
            var islemleri = await _db.IslemKayitlari
                .Where(i => i.KullaniciAdi == kullanici.KullaniciAdi)
                .OrderByDescending(i => i.Zaman)
                .Take(200)
                .ToListAsync();

            ViewBag.Girisler = girisleri;
            ViewBag.ToplamSure = girisleri.Aggregate(TimeSpan.Zero, (toplam, g) => toplam + g.Sure);
            return View(islemleri);
        }
    }
}
