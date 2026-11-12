using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;

namespace AracSatisSistemi.Api
{
    // Kayıt İşlemleri (Dosya Create/Edit) formunun ihtiyaç duyduğu tüm referans listelerini
    // (marka/model/cins/vergi dairesi/otopark/renk + model yılı üst sınırı) TEK istekte
    // döner - Razor tarafındaki DosyaController.DoldurListeler() ile aynı veriler.
    [ApiController]
    [Route("api/referans")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ReferansApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ReferansApiController(AppDbContext db) => _db = db;

        [HttpGet("kayit-formu")]
        public async Task<IActionResult> KayitFormu()
        {
            var cinsler = await _db.AracCinsleri.OrderBy(a => a.Ad).Select(a => new { a.Id, a.Ad }).ToListAsync();
            var markalar = await _db.AracMarkalari.OrderBy(a => a.Ad).Select(a => new { a.Id, a.Ad }).ToListAsync();
            var otoparklar = await _db.Otoparklar.OrderBy(a => a.Ad).Select(a => new { a.Id, a.Ad }).ToListAsync();
            var renkler = await _db.AracRenkleri.OrderBy(a => a.Ad).Select(a => new { a.Id, a.Ad }).ToListAsync();

            var genelAyar = await _db.GenelAyarlar.FirstOrDefaultAsync();
            var maksimumModelYili = genelAyar?.MaksimumModelYili > 0 ? genelAyar.MaksimumModelYili : 2027;

            var vergiDaireleri = await _db.VergiDaireleri.OrderBy(a => a.Sehir).ThenBy(a => a.Ad)
                .Select(v => new { v.Id, v.Ad, v.Sehir }).ToListAsync();
            var ankaraVergiDaireleri = vergiDaireleri.Where(v => v.Sehir == "Ankara").OrderBy(v => v.Ad).ToList();
            var sehirler = vergiDaireleri.Select(v => v.Sehir).Distinct().OrderBy(s => s).ToList();

            var aracModelleri = await _db.AracModelleri.OrderBy(m => m.Ad).ToListAsync();
            var modelSozlugu = aracModelleri.GroupBy(m => m.MarkaAdi).ToDictionary(g => g.Key, g => g.Select(m => m.Ad).ToList());

            return Ok(new
            {
                cinsler,
                markalar,
                otoparklar,
                renkler,
                maksimumModelYili,
                ankaraVergiDaireleri,
                vergiDaireleri,
                sehirler,
                modelSozlugu
            });
        }

        // Yeni dosya formu açılırken 1./2. Satış tarihi önerisi - DosyaController.Create
        // (GET) ile aynı mantık (Satış Takvimi'nden sonraki iki tarih, yoksa haftalık
        // Çarşamba hesaplaması) ve varsayılan komisyon bilgileri.
        [HttpGet("yeni-dosya-onerileri")]
        public async Task<IActionResult> YeniDosyaOnerileri()
        {
            var komisyon = await _db.KomisyonAyarlari.FirstOrDefaultAsync();
            var bugun = DateTime.Today;
            var sonrakiTarihler = await _db.SatisTarihleri
                .Where(t => t.Tarih >= bugun)
                .OrderBy(t => t.Tarih)
                .Take(2)
                .Select(t => t.Tarih)
                .ToListAsync();
            var resmiTatiller = await _db.ResmiTatiller.Select(t => t.Tarih.Date).ToListAsync();
            var satis1Onerisi = sonrakiTarihler.Count > 0
                ? sonrakiTarihler[0]
                : IhaleTarihHesaplayici.OnumuzdekiHaftaCarsambasi(bugun, resmiTatiller);
            var satis2Onerisi = sonrakiTarihler.Count > 1
                ? sonrakiTarihler[1]
                : IhaleTarihHesaplayici.SonrakiCarsamba(satis1Onerisi.AddDays(7), resmiTatiller);

            return Ok(new
            {
                satis1Onerisi,
                satis2Onerisi,
                komisyonBaskani = komisyon?.Baskan,
                komisyonUye1 = komisyon?.Uye1,
                komisyonUye2 = komisyon?.Uye2,
                komisyonUye3 = komisyon?.Uye3
            });
        }
    }
}
