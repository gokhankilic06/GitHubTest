using System.Text.Json;

namespace VerasetRandevu;

public record MesaiBlogu(string Baslangic, string Bitis);

public record BelgeAyari(string Ad, string Dosya);

// Kök dizindeki config.json dosyasının karşılığı (üç backend aynı dosyayı kullanır).
public record Ayarlar(
    string VergiDairesi,
    string SaatDilimi,
    List<string> Ilceler,
    List<MesaiBlogu> MesaiBloklari,
    int RandevuAraligiDakika,
    int SlotKapasitesi,
    int IleriGunSayisi,
    string YeniGunAcilisSaati,
    List<string> Tatiller,
    List<string> YarimGunler,
    BelgeAyari Belge)
{
    public static Ayarlar Yukle(string yol) =>
        JsonSerializer.Deserialize<Ayarlar>(File.ReadAllText(yol), new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException("config.json okunamadı.");

    // config.json dosyasını içeren proje kök dizinini bulur.
    public static string KokBul()
    {
        var ortam = Environment.GetEnvironmentVariable("VERASET_KOK");
        if (!string.IsNullOrEmpty(ortam)) return Path.GetFullPath(ortam);

        foreach (var baslangic in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dizin = new DirectoryInfo(baslangic); dizin != null; dizin = dizin.Parent)
            {
                if (File.Exists(Path.Combine(dizin.FullName, "config.json")) &&
                    File.Exists(Path.Combine(dizin.FullName, "schema.sql")))
                {
                    return dizin.FullName;
                }
            }
        }
        throw new InvalidOperationException("config.json bulunamadı. VERASET_KOK ortam değişkenini ayarlayın.");
    }
}
