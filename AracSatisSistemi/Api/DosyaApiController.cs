using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Api
{
    // React arayüzünün Kayıt İşlemleri / Dosya Ara-Listele / Dosya Detayı ekranlarının
    // konuştuğu JSON API. İş mantığı (dosya no üretimi, otomatik hesaplanan alanlar vb.)
    // DosyaController (Razor) ile BİREBİR aynı - tek, paylaşılan AppDbContext/Models
    // üzerinden çalıştığı için iki arayüz de aynı veritabanını, aynı kurallarla okur/yazar.
    [ApiController]
    [Route("api/dosyalar")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class DosyaApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        public DosyaApiController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string? arama, [FromQuery] string? durumFiltre)
        {
            var sorgu = _db.Dosyalar.Include(d => d.Satislar).AsQueryable();
            if (!string.IsNullOrWhiteSpace(arama))
            {
                sorgu = sorgu.Where(d => d.DosyaNo.Contains(arama)
                    || d.Plaka.Contains(arama)
                    || d.AdiSoyadiUnvani.Contains(arama));
            }
            if (durumFiltre == "aktif")
            {
                sorgu = sorgu.Where(d => d.Durum == DosyaDurumu.Kayitli || d.Durum == DosyaDurumu.SatisaCikti);
            }
            else if (durumFiltre == "pasif")
            {
                sorgu = sorgu.Where(d => d.Durum == DosyaDurumu.Satildi || d.Durum == DosyaDurumu.IptalEdildi
                    || d.Durum == DosyaDurumu.IadeEdildi || d.Durum == DosyaDurumu.Kapandi
                    || d.Durum == DosyaDurumu.AliciyaTerkEdildi);
            }

            var liste = await sorgu.OrderByDescending(d => d.KayitTarihi).ToListAsync();
            return Ok(liste.Select(DosyaOzeti));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var dosya = await _db.Dosyalar
                .Include(d => d.Satislar)
                .Include(d => d.IptalIade)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (dosya == null) return NotFound();
            return Ok(dosya);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Dosya dosya)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            dosya.KayitTarihi = DateTime.Now;
            dosya.Durum = DosyaDurumu.Kayitli;

            // İki kullanıcı aynı anda yeni dosya kaydederse çakışmaya karşı güvenli -
            // bkz. SeedData.DosyaNoAtaVeKaydetAsync.
            await SeedData.DosyaNoAtaVeKaydetAsync(_db, dosya);
            return CreatedAtAction(nameof(Details), new { id = dosya.Id }, dosya);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Edit(int id, Dosya guncel)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var dosya = await _db.Dosyalar.FindAsync(id);
            if (dosya == null) return NotFound();

            dosya.VergiDairesiAdi = guncel.VergiDairesiAdi;
            dosya.VergiDairesiYaziTarihi = guncel.VergiDairesiYaziTarihi;
            dosya.VergiDairesiYaziSayisi = guncel.VergiDairesiYaziSayisi;
            dosya.AlacakliVergiDairesi = guncel.AlacakliVergiDairesi;
            dosya.VergiKimlikNumarasi = guncel.VergiKimlikNumarasi;
            dosya.TCKimlikNumarasi = guncel.TCKimlikNumarasi;
            dosya.AdiSoyadiUnvani = guncel.AdiSoyadiUnvani;
            dosya.KanuniTemsilci = guncel.KanuniTemsilci;
            dosya.Plaka = guncel.Plaka;
            dosya.AracCinsi = guncel.AracCinsi;
            dosya.AracMarkasi = guncel.AracMarkasi;
            dosya.AracTipi = guncel.AracTipi;
            dosya.ModelYili = guncel.ModelYili;
            dosya.Renk = guncel.Renk;
            dosya.RuhsatVar = guncel.RuhsatVar;
            dosya.AnahtarVar = guncel.AnahtarVar;
            dosya.CekiciVar = guncel.CekiciVar;
            dosya.OtoparkAdi = guncel.OtoparkAdi;
            dosya.HasarDurumu = guncel.HasarDurumu;
            dosya.MuhammenBedel = guncel.MuhammenBedel;
            dosya.KdvOrani = guncel.KdvOrani;
            dosya.DamgaVergisiOrani = guncel.DamgaVergisiOrani;
            dosya.OtoparkaGirisTarihi = guncel.OtoparkaGirisTarihi;
            dosya.KomisyonBaskani = guncel.KomisyonBaskani;
            dosya.KomisyonUye1 = guncel.KomisyonUye1;
            dosya.KomisyonUye2 = guncel.KomisyonUye2;
            dosya.KomisyonUye3 = guncel.KomisyonUye3;
            dosya.Satis1Tarihi = guncel.Satis1Tarihi;
            dosya.Satis2Tarihi = guncel.Satis2Tarihi;

            await _db.SaveChangesAsync();
            return Ok(dosya);
        }

        // Liste ekranı için ağır Satislar koleksiyonunu tam döndürmek yerine, listede
        // gösterilen alanlarla sınırlı küçük bir özet - Asama/AsamaGorunen gibi hesaplanan
        // alanlar Satislar yüklü olduğu sürece doğru çalışır.
        private static object DosyaOzeti(Dosya d) => new
        {
            d.Id,
            d.DosyaNo,
            d.KayitTarihi,
            d.Plaka,
            d.AdiSoyadiUnvani,
            d.VergiDairesiAdi,
            d.AracMarkasi,
            d.AracCinsi,
            d.Durum,
            DurumGorunenAd = d.DurumGorunenAd,
            GenelDurumGorunen = d.GenelDurumGorunen,
            AsamaGorunen = d.AsamaGorunen,
            AsamaRozetSinifi = d.AsamaRozetSinifi,
            Aktif = d.Aktif
        };
    }
}
