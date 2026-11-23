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
            })
            .OrderByDescending(o => o.SuAnOnlineMi)
            .ThenByDescending(o => o.SonGiris)
            .ToList();

            // Genel özet (tüm kullanıcılar) - sayfanın en üstündeki kutular için.
            var bugun = DateTime.Today;
            ViewBag.ToplamKullanici = ozetler.Count;
            ViewBag.SuAnOnline = ozetler.Count(o => o.SuAnOnlineMi);
            ViewBag.BugunGirisYapan = tumGirisler.Where(g => g.GirisZamani.Date == bugun).Select(g => g.KullaniciId).Distinct().Count();
            ViewBag.ToplamIslem = islemSayilari.Values.Sum();

            return View(ozetler);
        }

        public async Task<IActionResult> Detay(int id)
        {
            var kullanici = await _db.Kullanicilar.FindAsync(id);
            if (kullanici == null) return NotFound();

            // İstatistikler (toplam giriş sayısı, ortalama süre, günlük dağılım vb.) kullanıcının
            // TÜM geçmişinden hesaplanır; aşağıdaki tablolar ise görüntüleme amacıyla son 200
            // kayıtla sınırlıdır - ikisi birbirinden bağımsızdır.
            var tumGirisler = await _db.GirisKayitlari
                .Where(g => g.KullaniciId == id)
                .OrderByDescending(g => g.GirisZamani)
                .ToListAsync();
            var tumIslemler = await _db.IslemKayitlari
                .Where(i => i.KullaniciAdi == kullanici.KullaniciAdi)
                .OrderByDescending(i => i.Zaman)
                .ToListAsync();

            ViewBag.Kullanici = kullanici;
            ViewBag.Girisler = tumGirisler.Take(200).ToList();

            var toplamSure = tumGirisler.Aggregate(TimeSpan.Zero, (toplam, g) => toplam + g.Sure);
            ViewBag.ToplamSure = toplamSure;
            ViewBag.OrtalamaSure = tumGirisler.Count > 0 ? toplamSure / tumGirisler.Count : TimeSpan.Zero;
            ViewBag.IlkGiris = tumGirisler.Count > 0 ? tumGirisler.Min(g => g.GirisZamani) : (DateTime?)null;
            ViewBag.SonGiris = tumGirisler.Count > 0 ? tumGirisler.Max(g => g.GirisZamani) : (DateTime?)null;
            ViewBag.SonIslem = tumIslemler.Count > 0 ? tumIslemler.Max(i => i.Zaman) : (DateTime?)null;
            ViewBag.SuAnOnlineMi = tumGirisler.Any(g => g.CikisZamani == null && (DateTime.Now - g.SonAktiviteZamani) < TimeSpan.FromMinutes(15));

            // Son 14 gün - her gün kaç kez giriş yapıldığı (çubuk grafik için).
            var sonGunler = Enumerable.Range(0, 14).Select(i => DateTime.Today.AddDays(-13 + i)).ToList();
            ViewBag.GunlukGirisEtiketleri = sonGunler.Select(g => g.ToString("dd.MM")).ToList();
            ViewBag.GunlukGirisSayilari = sonGunler
                .Select(g => tumGirisler.Count(giris => giris.GirisZamani.Date == g))
                .ToList();

            // İşlem türü dağılımı - en çok yapılan ilk 8 işlem (pasta grafik + tablo için).
            ViewBag.IslemTuruDagilimi = tumIslemler
                .GroupBy(i => i.Detay ?? $"{i.Controller}/{i.Action}")
                .Select(g => new { Etiket = g.Key, Adet = g.Count() })
                .OrderByDescending(x => x.Adet)
                .Take(8)
                .ToList();

            ViewBag.ToplamGiris = tumGirisler.Count;
            ViewBag.ToplamIslem = tumIslemler.Count;

            return View(tumIslemler.Take(200).ToList());
        }
    }
}
