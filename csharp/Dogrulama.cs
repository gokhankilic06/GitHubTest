using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace VerasetRandevu;

public class RandevuVerisi
{
    public string Ilce { get; set; } = "";
    public string Tarih { get; set; } = "";
    public string Saat { get; set; } = "";
    public string VefatEdenAd { get; set; } = "";
    public string VefatEdenSoyad { get; set; } = "";
    public string VefatEdenTc { get; set; } = "";
    public string BasvuranAd { get; set; } = "";
    public string BasvuranSoyad { get; set; } = "";
    public int? BasvuranDogumYili { get; set; }
    public string BasvuranTc { get; set; } = "";
    public string BasvuranCep { get; set; } = "";
}

public static partial class Dogrulama
{
    private static readonly CultureInfo Turkce = new("tr-TR");

    [GeneratedRegex(@"\A[A-Za-zÇĞİÖŞÜçğıöşüÂâÎîÛû .'-]{2,50}\z")]
    private static partial Regex IsimDeseni();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Bosluklar();

    [GeneratedRegex(@"\A5[0-9]{9}\z")]
    private static partial Regex CepDeseni();

    public static bool TcGecerliMi(string? tc)
    {
        if (tc is not { Length: 11 } || tc[0] == '0' || !tc.All(c => c is >= '0' and <= '9')) return false;
        var d = tc.Select(c => c - '0').ToArray();
        var tek = d[0] + d[2] + d[4] + d[6] + d[8];
        var cift = d[1] + d[3] + d[5] + d[7];
        if (((tek * 7 - cift) % 10 + 10) % 10 != d[9]) return false;
        return d.Take(10).Sum() % 10 == d[10];
    }

    private static string Metin(JsonElement g, string alan) =>
        g.ValueKind == JsonValueKind.Object && g.TryGetProperty(alan, out var e) && e.ValueKind == JsonValueKind.String
            ? Bosluklar().Replace(e.GetString()!.Trim(), " ")
            : "";

    private static int? Yil(JsonElement g)
    {
        if (g.ValueKind != JsonValueKind.Object || !g.TryGetProperty("basvuranDogumYili", out var e)) return null;
        if (e.ValueKind == JsonValueKind.Number)
        {
            if (e.TryGetInt32(out var i)) return i;
            if (e.TryGetDouble(out var d) && d == Math.Floor(d) && Math.Abs(d) < int.MaxValue) return (int)d;
            return null;
        }
        if (e.ValueKind == JsonValueKind.String)
        {
            var s = e.GetString()!.Trim();
            if (s.Length is >= 1 and <= 4 && s.All(c => c is >= '0' and <= '9')) return int.Parse(s, CultureInfo.InvariantCulture);
        }
        return null;
    }

    // Formdan gelen randevu verisini doğrular ve normalleştirir.
    // hatalar boşsa veri kaydedilmeye hazırdır.
    public static (RandevuVerisi veri, Dictionary<string, string> hatalar) RandevuDogrula(
        JsonElement g, IReadOnlyCollection<string> ilceler, int buYil)
    {
        var hatalar = new Dictionary<string, string>();
        var veri = new RandevuVerisi();

        if (!(g.ValueKind == JsonValueKind.Object && g.TryGetProperty("taahhut", out var t) && t.ValueKind == JsonValueKind.True))
        {
            hatalar["taahhut"] = "Açıklamaları okuduğunuzu onaylamanız gerekmektedir.";
        }

        veri.Ilce = Metin(g, "ilce");
        if (!ilceler.Contains(veri.Ilce)) hatalar["ilce"] = "Lütfen geçerli bir ilçe seçiniz.";

        veri.Tarih = Metin(g, "tarih");
        veri.Saat = Metin(g, "saat");

        string Isim(string alan, string ad)
        {
            var deger = Metin(g, alan);
            if (deger.Length == 0) hatalar[alan] = $"{ad} alanı zorunludur.";
            else if (!IsimDeseni().IsMatch(deger)) hatalar[alan] = $"{ad} yalnızca harflerden oluşmalı (2-50 karakter).";
            return deger.ToUpper(Turkce);
        }
        veri.VefatEdenAd = Isim("vefatEdenAd", "Ad");
        veri.VefatEdenSoyad = Isim("vefatEdenSoyad", "Soyad");
        veri.BasvuranAd = Isim("basvuranAd", "Ad");
        veri.BasvuranSoyad = Isim("basvuranSoyad", "Soyad");

        veri.VefatEdenTc = Metin(g, "vefatEdenTc");
        veri.BasvuranTc = Metin(g, "basvuranTc");
        if (!TcGecerliMi(veri.VefatEdenTc)) hatalar["vefatEdenTc"] = "Geçerli bir T.C. Kimlik Numarası giriniz.";
        if (!TcGecerliMi(veri.BasvuranTc)) hatalar["basvuranTc"] = "Geçerli bir T.C. Kimlik Numarası giriniz.";
        else if (veri.BasvuranTc == veri.VefatEdenTc)
        {
            hatalar["basvuranTc"] = "Randevuyu alanın T.C. numarası vefat edenle aynı olamaz.";
        }

        veri.BasvuranDogumYili = Yil(g);
        if (veri.BasvuranDogumYili is not { } yil || yil < 1900 || yil > buYil)
        {
            hatalar["basvuranDogumYili"] = "Geçerli bir doğum yılı giriniz (örn: 1980).";
        }

        var cep = new string(Metin(g, "basvuranCep").Where(c => c is >= '0' and <= '9').ToArray());
        if (cep.Length == 11 && cep.StartsWith("05", StringComparison.Ordinal)) cep = cep[1..];
        veri.BasvuranCep = cep;
        if (!CepDeseni().IsMatch(cep))
        {
            hatalar["basvuranCep"] = "Cep numarasını başında 0 olmadan 5xxxxxxxxx şeklinde giriniz.";
        }

        return (veri, hatalar);
    }
}
