using System.IO;
using System.Text.Json;

namespace AracSatisSistemiWinForms
{
    // "api-ayarlari.json" dosyasından sunucu adresini okur/yazar - böylece ofisteki
    // her bilgisayarda, programı yeniden derlemeden, Güncellemeler ekranından ayarlanan
    // gerçek sunucu adresine (IP) işaret edilebilir.
    public static class Ayarlar
    {
        private static readonly string DosyaYolu = Path.Combine(AppContext.BaseDirectory, "api-ayarlari.json");

        public static string ApiAdresiniOku()
        {
            try
            {
                if (File.Exists(DosyaYolu))
                {
                    var icerik = File.ReadAllText(DosyaYolu);
                    var ayar = JsonSerializer.Deserialize<AyarVerisi>(icerik, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    if (!string.IsNullOrWhiteSpace(ayar?.ApiAdresi)) return ayar.ApiAdresi;
                }
            }
            catch
            {
                // Dosya bozuksa/okunamazsa varsayılana dönülür.
            }
            return "http://localhost:5080";
        }

        public static void ApiAdresiniKaydet(string adres)
        {
            var ayar = new AyarVerisi { ApiAdresi = adres };
            File.WriteAllText(DosyaYolu, JsonSerializer.Serialize(ayar, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
        }

        private class AyarVerisi
        {
            public string ApiAdresi { get; set; } = "http://localhost:5080";
        }
    }
}
