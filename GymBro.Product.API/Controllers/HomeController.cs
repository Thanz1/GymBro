using Microsoft.AspNetCore.Mvc;

namespace GymBro.Product.API.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
