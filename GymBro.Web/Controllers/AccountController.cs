using GymBro.Contracts;
using GymBro.Service;
using GymBro.Web.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IIdentityService _identityService;
        private readonly IOrderService _orderService; // THÊM: Để lấy lịch sử đơn hàng

        public AccountController(IIdentityService identityService, IOrderService orderService)
        {
            _identityService = identityService;
            _orderService = orderService;
        }

        // ... (Register giữ nguyên vì đã chuẩn) ...

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var user = await _identityService.LoginAsync(loginDto);
            if (user != null)
            {
                // QUAN TRỌNG: Lưu cả đối tượng User (chứa Token) vào Session
                HttpContext.Session.SetObject("User", user);

                // Nếu UserDto của bạn có thuộc tính Token, hãy lưu riêng để dễ dùng
                if (!string.IsNullOrEmpty(user.Token))
                {
                    HttpContext.Session.SetString("JWToken", user.Token);
                }

                if (user.Role == "Admin") return RedirectToAction("Index", "Products");
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ErrorMessage = "Sai tên đăng nhập hoặc mật khẩu.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Xóa sạch Session cho an toàn
            return RedirectToAction("Index", "Home");
        }

        // SỬA: Lấy lịch sử đơn hàng từ API Port 7003
        public async Task<IActionResult> OrderHistory()
        {
            var userSession = HttpContext.Session.GetObject<UserDto>("User");
            if (userSession == null) return RedirectToAction("Login");

            // Gọi API lấy đơn hàng của User này[cite: 1]
            var orders = await _orderService.GetOrdersByUserIdAsync(userSession.Id);
            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int? id)
        {
            if (id == null) return NotFound();
            var userSession = HttpContext.Session.GetObject<UserDto>("User");
            if (userSession == null) return RedirectToAction("Login");

            var order = await _orderService.GetOrderDetailsAsync(id.Value);
            return View(order);
        }
    }
}