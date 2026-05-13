using GymBro.Contracts;
using GymBro.Contracts.DTOs;
using GymBro.Service;
using GymBro.Web.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IIdentityService _identityService;
        private readonly IOrderService _orderService;

        public AccountController(IIdentityService identityService, IOrderService orderService)
        {
            _identityService = identityService;
            _orderService = orderService;
        }

        // --- ĐĂNG NHẬP ---

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            // Identity API phía sau sẽ tự kiểm tra xem loginDto.Username là Tên hay Email
            var user = await _identityService.LoginAsync(loginDto);

            if (user != null)
            {
                // Lưu thông tin vào Session để dùng cho toàn trang Web
                HttpContext.Session.SetObject("User", user);

                if (!string.IsNullOrEmpty(user.Token))
                {
                    HttpContext.Session.SetString("JWToken", user.Token);
                }

                if (user.Role == "Admin") return RedirectToAction("Index", "Products");
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ErrorMessage = "Sai tài khoản hoặc mật khẩu.";
            return View();
        }

        // --- QUÊN MẬT KHẨU ---

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(); // Trả về trang ForgotPassword.cshtml đã tạo
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto forgotPasswordDto)
        {
            // Gửi Identifier (có thể là Username hoặc Email) sang API để xử lý
            var result = await _identityService.ForgotPasswordAsync(forgotPasswordDto);

            if (result)
            {
                ViewBag.SuccessMessage = "Nếu thông tin chính xác, chúng tôi đã gửi hướng dẫn khôi phục.";
                return View();
            }

            ViewBag.ErrorMessage = "Có lỗi xảy ra, vui lòng thử lại sau.";
            return View();
        }

        // --- ĐĂNG XUẤT & KHÁC ---

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> OrderHistory()
        {
            var userSession = HttpContext.Session.GetObject<UserDto>("User");
            if (userSession == null) return RedirectToAction("Login");

            var orders = await _orderService.GetOrdersByUserIdAsync(userSession.Id);
            return View(orders);
        }
        [HttpGet]
        public IActionResult ResetPassword(string identifier)
        {
            // Trả về View để người dùng nhập mật khẩu mới
            return View(new ResetPasswordDto { Identifier = identifier });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var result = await _identityService.ResetPasswordAsync(resetPasswordDto);
            if (result)
            {
                TempData["SuccessMessage"] = "Mật khẩu đã được đổi thành công. Hãy đăng nhập lại.";
                return RedirectToAction("Login");
            }
            ViewBag.ErrorMessage = "Không thể đổi mật khẩu, vui lòng thử lại.";
            return View(resetPasswordDto);
        }
    }
}