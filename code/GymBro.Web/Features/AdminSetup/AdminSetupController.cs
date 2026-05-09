using GymBro.Application.Auth;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class AdminSetupController : Controller
    {
        private readonly GymBroDbContext _context;
        private readonly IUserAuthenticationService _authenticationService;

        public AdminSetupController(
            GymBroDbContext context,
            IUserAuthenticationService authenticationService)
        {
            _context = context;
            _authenticationService = authenticationService;
        }

        [HttpGet]
        public async Task<IActionResult> CreateAdmin()
        {
            if (await _context.Users.AnyAsync(u => u.Role == "Admin"))
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(
            string username,
            string password,
            string fullName,
            string email,
            string confirmPassword)
        {
            if (await _context.Users.AnyAsync(u => u.Role == "Admin"))
            {
                ViewBag.Message = "Hệ thống đã có Admin. Không thể tạo thêm.";
                ViewBag.MessageType = "danger";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Message = "Mật khẩu xác nhận không khớp.";
                ViewBag.MessageType = "danger";
                return View();
            }

            var result = await _authenticationService.RegisterAsync(new RegisterUserRequest
            {
                Username = username,
                Password = password,
                FullName = string.IsNullOrWhiteSpace(fullName) ? "Administrator" : fullName,
                Email = email,
                Address = "System Admin",
                Role = "Admin",
                IsActive = true
            });

            if (!result.Succeeded)
            {
                ViewBag.Message = result.Errors.Values.SelectMany(messages => messages).FirstOrDefault()
                    ?? "Tạo tài khoản Admin thất bại.";
                ViewBag.MessageType = "danger";
                return View();
            }

            ViewBag.Message = "Tạo tài khoản Admin thành công! Bạn có thể đăng nhập ngay.";
            ViewBag.MessageType = "success";
            ModelState.Clear();

            return View();
        }
    }
}
