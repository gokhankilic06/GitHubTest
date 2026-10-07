using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Data
{
    // İhaleyi kazanan alıcı, satış tarihinden sonraki 3 iş günü (hafta sonu ve resmi
    // tatiller hariç) içinde idareye gelip "Alıcı Ödeme Bilgileri" işlemini
    // tamamlamazsa (VerilenSure hâlâ boşsa), o satış sonucu otomatik olarak
    // "Alıcı Çıkmadı"ya döner; dosya böylece bir sonraki satış aşamasının
    // (1.Satış -> 2.Satış -> Pazarlık -> 6183/86) listesinde otomatik olarak yer alır.
    public static class OtomatikSurecIsleyici
    {
        public static async Task AliciGelmeyenSatislariGuncelleAsync(AppDbContext db)
        {
            var bekleyenler = await db.Satislar
                .Include(s => s.Dosya)
                .Where(s => s.SatisSonucu == SatisSonucu.Satildi && s.VerilenSure == null)
                .Where(s => s.Dosya != null && s.Dosya.Durum == DosyaDurumu.Satildi)
                .ToListAsync();

            if (bekleyenler.Count == 0) return;

            var resmiTatiller = await db.ResmiTatiller.Select(t => t.Tarih.Date).ToListAsync();
            var bugun = DateTime.Today;
            var degisti = false;

            foreach (var satis in bekleyenler)
            {
                var sonGun = IhaleTarihHesaplayici.IsGunuEkle(satis.SatisTarihi, 3, resmiTatiller);
                if (bugun <= sonGun) continue;

                satis.SatisSonucu = SatisSonucu.AliciCikmadi;
                satis.SatisBedeli = null;
                satis.Dosya!.Durum = DosyaDurumu.SatisaCikti;
                degisti = true;
            }

            if (degisti) await db.SaveChangesAsync();
        }
    }
}
