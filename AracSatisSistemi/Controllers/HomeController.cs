using Microsoft.AspNetCore.Mvc;

namespace AracSatisSistemi.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Hata()
        {
            return View();
        }
    }
}
