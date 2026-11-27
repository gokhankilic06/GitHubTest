using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Services
{
    // Pazarlık/6183-86 Komisyon Oturumu oluştururken, o oturumda görüşülecek dosya(lar)ı
    // seçebilmek için - bkz. KomisyonController, PazarlikController.
    public static class PazarlikDosyaYardimcisi
    {
        private static readonly Dictionary<SatisTuru, Dosya.SurecAsamasi> BeklenenAsama = new()
        {
            [SatisTuru.Pazarlik] = Dosya.SurecAsamasi.PazarlikBekliyor,
            [SatisTuru.Madde6183_86] = Dosya.SurecAsamasi.Madde6183_86Bekliyor
        };

        public static async Task<List<Dosya>> BekleyenDosyalariGetir(AppDbContext db, SatisTuru tur)
        {
            var aktifDosyalar = await db.Dosyalar.Include(d => d.Satislar)
                .Where(d => d.Durum == DosyaDurumu.Kayitli || d.Durum == DosyaDurumu.SatisaCikti)
                .ToListAsync();
            var beklenen = BeklenenAsama[tur];
            return aktifDosyalar.Where(d => d.Asama == beklenen).OrderBy(d => d.DosyaNo).ToList();
        }
    }
}
