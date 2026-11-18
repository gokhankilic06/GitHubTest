using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Models;

namespace AracSatisSistemi.Data
{
    // Bir dosyanın gösterilecek satışına (varsa "Satıldı" sonuçlanan, yoksa en son girilen)
    // ait mali özeti (KDV, Damga Vergisi, Toplam, Otopark Ücreti, Kalan Tutar) hesaplar.
    // Hem Yazışma ekranında hem de Tutanak ve Raporlar'daki Araç Satış Kararı belgesinde
    // aynı tek hesaplama kaynağı kullanılsın diye buraya alındı.
    public record SatisMaliOzet(
        Dosya Dosya,
        Satis? Satis,
        decimal SatisBedeli,
        decimal KdvTutari,
        decimal DvTutari,
        decimal ToplamTutar,
        decimal OtoparkTutari,
        decimal KalanTutar);

    public static class SatisMaliOzetHesaplayici
    {
        public static async Task<SatisMaliOzet?> Getir(AppDbContext db, string dosyaNo)
        {
            var dosya = await db.Dosyalar.Include(d => d.Satislar).FirstOrDefaultAsync(d => d.DosyaNo == dosyaNo);
            if (dosya == null) return null;
            return await Getir(db, dosya);
        }

        public static async Task<SatisMaliOzet> Getir(AppDbContext db, Dosya dosya)
        {
            // Gösterilecek satış kaydı: varsa "Satıldı" sonuçlanan, yoksa en son girilen.
            var satis = dosya.Satislar.FirstOrDefault(s => s.SatisSonucu == SatisSonucu.Satildi)
                ?? dosya.Satislar.OrderByDescending(s => s.SatisTarihi).FirstOrDefault();

            var genelAyar = await db.GenelAyarlar.FirstOrDefaultAsync() ?? new GenelAyar();
            var aracCinsi = await db.AracCinsleri.FirstOrDefaultAsync(c => c.Ad == dosya.AracCinsi);
            var kategori = aracCinsi?.OtoparkKategorisi ?? OtoparkAracKategorisi.KucukArac;

            // Çekici bedeli henüz Alıcı Ödeme Bilgileri ekranından girilmemişse (bu ekran
            // satıştan sonra, Yazışma/Araç Satış Kararı'ndan DAHA GEÇ doldurulabilir), dosyada
            // "Çekici Var" işaretliyse sabit kategori ücreti geçici olarak (DB'ye kaydetmeden,
            // yalnızca bu istek için) kullanılır - aksi halde buradan üretilen otopark tutarı
            // ve Yazışma belgelerindeki çekici ücreti satırı hep 0 TL görünürdü.
            if (satis != null && satis.CekiciBedeli == null && dosya.CekiciVar)
            {
                satis.CekiciBedeli = genelAyar.CekiciUcreti(kategori);
            }

            decimal satisBedeli = satis?.SatisBedeli ?? 0m;
            decimal kdvTutari = Math.Round(satisBedeli * dosya.KdvOrani / 100m, 2);
            decimal dvTutari = Math.Round(satisBedeli * dosya.DamgaVergisiOrani / 1000m, 2);
            decimal toplamTutar = satisBedeli + kdvTutari + dvTutari;
            decimal otoparkTutari = satis != null
                ? OtoparkHesaplayici.OtoparkTavaniUygula(
                    OtoparkHesaplayici.OtoparkUcretiHesapla(satis.OtoparkGunSayisi, kategori, genelAyar),
                    satis.CekiciBedeli ?? 0m, satis.SatisBedeli)
                : 0m;
            decimal kalanTutar = satisBedeli - otoparkTutari;

            return new SatisMaliOzet(dosya, satis, satisBedeli, kdvTutari, dvTutari, toplamTutar, otoparkTutari, kalanTutar);
        }
    }
}
