using Microsoft.AspNetCore.Mvc;

namespace AracSatisSistemi.Controllers
{
    // "Yedekleme" ekranı: Services/YedeklemeServisi.cs tarafından otomatik alınan
    // günlük/haftalık/aylık veritabanı yedeklerinin listesini gösterir ve indirilebilir
    // yapar. Yedekler kendisi burada ALINMAZ (bu iş arka planda otomatik yapılır); bu
    // ekran sadece var olan yedekleri görüntülemek/indirmek içindir.
    public class YedeklemeController : Controller
    {
        private readonly IWebHostEnvironment _env;
        public YedeklemeController(IWebHostEnvironment env) => _env = env;

        public class YedekDosyasi
        {
            public string Ad { get; set; } = "";
            public DateTime Tarih { get; set; }
            public long BoyutBayt { get; set; }
        }

        private string KokKlasor => Path.Combine(_env.ContentRootPath, "Backups");

        private static List<YedekDosyasi> KlasorYedekleri(string klasorYolu)
        {
            if (!Directory.Exists(klasorYolu)) return new List<YedekDosyasi>();
            return Directory.GetFiles(klasorYolu, "*.db")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .Select(f => new YedekDosyasi { Ad = f.Name, Tarih = f.LastWriteTime, BoyutBayt = f.Length })
                .ToList();
        }

        public IActionResult Index()
        {
            ViewBag.Gunluk = KlasorYedekleri(Path.Combine(KokKlasor, "Gunluk"));
            ViewBag.Haftalik = KlasorYedekleri(Path.Combine(KokKlasor, "Haftalik"));
            ViewBag.Aylik = KlasorYedekleri(Path.Combine(KokKlasor, "Aylik"));
            return View();
        }

        // klasor: "Gunluk" | "Haftalik" | "Aylik". Dosya adı, klasör dışına çıkılamaması için
        // yalnızca dosya adı (yol ayracı olmadan) olarak doğrulanır.
        public IActionResult Indir(string klasor, string dosya)
        {
            if (klasor != "Gunluk" && klasor != "Haftalik" && klasor != "Aylik") return NotFound();
            if (string.IsNullOrWhiteSpace(dosya) || dosya.Contains('/') || dosya.Contains('\\') || dosya.Contains(".."))
                return NotFound();

            var klasorYolu = Path.Combine(KokKlasor, klasor);
            var tamYol = Path.Combine(klasorYolu, dosya);
            if (!System.IO.File.Exists(tamYol)) return NotFound();

            var bayt = System.IO.File.ReadAllBytes(tamYol);
            return File(bayt, "application/octet-stream", dosya);
        }
    }
}
