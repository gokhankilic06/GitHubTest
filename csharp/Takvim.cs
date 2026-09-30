using System.Globalization;

namespace VerasetRandevu;

public record Pencere(string Bugun, string Ilk, string Son);

// Tarih/saat kuralları. Tüm tarihler "yyyy-MM-dd", saatler "HH:mm" metinleridir.
public class Takvim(Ayarlar ayarlar)
{
    private readonly TimeZoneInfo _saatDilimi = TimeZoneInfo.FindSystemTimeZoneById(ayarlar.SaatDilimi);

    private static int Dakikaya(string saat)
    {
        var p = saat.Split(':');
        return int.Parse(p[0], CultureInfo.InvariantCulture) * 60 + int.Parse(p[1], CultureInfo.InvariantCulture);
    }

    private static string Saate(int dakika) => $"{dakika / 60:D2}:{dakika % 60:D2}";

    private static DateOnly Coz(string tarih) =>
        DateOnly.ParseExact(tarih, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Yaz(DateOnly tarih) => tarih.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string GunEkle(string tarih, int gun) => Yaz(Coz(tarih).AddDays(gun));

    public static bool GecerliTarihMi(string? tarih) =>
        tarih != null && tarih.Length == 10 &&
        DateOnly.TryParseExact(tarih, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    // Randevu alınabilecek ilk ve son gün. Aynı gün için randevu verilmez;
    // her gün YeniGunAcilisSaati'nde pencerenin sonuna yeni bir gün eklenir.
    public Pencere Pencere(DateTimeOffset an)
    {
        var yerel = TimeZoneInfo.ConvertTime(an, _saatDilimi);
        var bugun = Yaz(DateOnly.FromDateTime(yerel.DateTime));
        var acildi = yerel.Hour * 60 + yerel.Minute >= Dakikaya(ayarlar.YeniGunAcilisSaati);
        return new Pencere(bugun, GunEkle(bugun, 1), GunEkle(bugun, ayarlar.IleriGunSayisi - (acildi ? 0 : 1)));
    }

    // Kurum çalışıyor mu? (hafta sonu ve resmi tatiller hariç)
    public bool IsGunuMu(string tarih)
    {
        var gun = Coz(tarih).DayOfWeek;
        return gun != DayOfWeek.Saturday && gun != DayOfWeek.Sunday && !ayarlar.Tatiller.Contains(tarih);
    }

    // Verilen gün randevu penceresinde ve iş günü mü?
    public bool GunAcikMi(string? tarih, Pencere p) =>
        GecerliTarihMi(tarih) &&
        string.CompareOrdinal(tarih, p.Ilk) >= 0 &&
        string.CompareOrdinal(tarih, p.Son) <= 0 &&
        IsGunuMu(tarih!);

    // Verilen iş gününün randevu saatleri. Yarım günlerde yalnızca ilk mesai bloğu kullanılır.
    public List<string> GunSaatleri(string tarih)
    {
        var saatler = new List<string>();
        if (!IsGunuMu(tarih)) return saatler;
        var bloklar = ayarlar.YarimGunler.Contains(tarih) ? ayarlar.MesaiBloklari.Take(1) : ayarlar.MesaiBloklari;
        foreach (var blok in bloklar)
        {
            for (var d = Dakikaya(blok.Baslangic); d < Dakikaya(blok.Bitis); d += ayarlar.RandevuAraligiDakika)
            {
                saatler.Add(Saate(d));
            }
        }
        return saatler;
    }
}
