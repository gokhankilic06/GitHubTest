using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Data
{
    // Her istekte, o oturuma ait GirisKaydi'nın "son aktivite" zamanını günceller - bu
    // sayede tarayıcısını kapatıp "Çıkış" yapmadan ayrılan bir kullanıcı için de
    // Kullanıcılar (istatistik) sayfasında makul bir "ne kadar durdu" süresi hesaplanabilir
    // (bkz. GirisKaydi.Sure). Her istekte yazmamak için en az 1 dakikada bir güncellenir.
    public static class KullaniciAktiviteIzleyici
    {
        public static async Task GuncelleAsync(AppDbContext db, int girisKaydiId)
        {
            var kayit = await db.GirisKayitlari.FindAsync(girisKaydiId);
            if (kayit == null || kayit.CikisZamani != null) return;

            var simdi = DateTime.Now;
            if ((simdi - kayit.SonAktiviteZamani) < TimeSpan.FromMinutes(1)) return;

            kayit.SonAktiviteZamani = simdi;
            await db.SaveChangesAsync();
        }
    }
}
