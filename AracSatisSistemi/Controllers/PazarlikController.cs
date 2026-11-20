using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AracSatisSistemi.Data;

namespace AracSatisSistemi.Controllers
{
    // PAZARLIK VE 6183/86 TEK SATIŞ ekranı: bu iki satış türünün, 1./2. Satış'ın aksine
    // Satış Takvimi'nde sabit bir tarihi olmadığından, herhangi bir tarih/saat girilerek
    // komisyon oturumu açılabildiği kendi sayfası. Kayıt, Komisyon/Create action'ı
    // üzerinden aynı KomisyonOturumu tablosuna yazılır (bkz. KomisyonController).
    public class PazarlikController : Controller
    {
        private readonly AppDbContext _db;
        public PazarlikController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.VarsayilanKomisyon = await _db.KomisyonAyarlari.FirstOrDefaultAsync();
            return View();
        }
    }
}
