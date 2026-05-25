using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    [Route("Admin/Chat")]
    public class AdminChatController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View(); // Trả về giao diện chat
        }
    }
}
