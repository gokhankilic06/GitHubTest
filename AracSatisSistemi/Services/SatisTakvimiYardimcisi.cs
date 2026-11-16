using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Services
{
    // Satış Takvimi görünümü (SatisTakvimi/Index ve Ayarlar/Index'teki gömülü takvim)
    // tarafından ortak kullanılan yardımcı hesaplamalar.
    public static class SatisTakvimiYardimcisi
    {
        // Her takvim tarihi için o tarihte 1./2. Satış'ını BEKLEYEN (henüz sonucu
        // girilmemiş) dosya sayısını hesaplar; takvimi "Satış Sonuçlarını Gir" akışına
        // doğrudan bağlayan sayaç + bağlantı için kullanılır.
        public static async Task<(Dictionary<DateTime, int> Birinci, Dictionary<DateTime, int> Ikinci)> PlanliDosyaSayilariGetir(AppDbContext db)
        {
            var aktifDosyalar = await db.Dosyalar.Include(d => d.Satislar)
                .Where(d => d.Durum == DosyaDurumu.Kayitli || d.Durum == DosyaDurumu.SatisaCikti)
                .ToListAsync();

            var birinci = aktifDosyalar.Where(d => d.Asama == Dosya.SurecAsamasi.BirinciSatisBekliyor)
                .GroupBy(d => d.Satis1Tarihi.Date)
                .ToDictionary(g => g.Key, g => g.Count());
            var ikinci = aktifDosyalar.Where(d => d.Asama == Dosya.SurecAsamasi.IkinciSatisBekliyor)
                .GroupBy(d => d.Satis2Tarihi.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            return (birinci, ikinci);
        }
    }
}
