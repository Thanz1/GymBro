using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class AdminSetupController : Controller
    {
        private readonly GymBroDbContext _context;

        public AdminSetupController(GymBroDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> CreateAdmin()
        {
            // Kiểm tra bảo mật: Nếu đã có Admin rồi thì không cho tạo nữa -> Đá về trang đăng nhập
            if (await _context.Users.AnyAsync(u => u.Role == "Admin"))
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(string username, string password, string fullName, string email, string confirmPassword)
        {
            // Kiểm tra lại lần nữa cho chắc
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

            // Tạo User Admin
            var admin = new User
            {
                Username = username,
                // Mã hóa mật khẩu
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = fullName ?? "Administrator",
                Email = email,
                Role = "Admin", // Quan trọng nhất là dòng này
                CreatedDate = DateTime.Now,
                Address = "System Admin"
            };

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            ViewBag.Message = "Tạo tài khoản Admin thành công! Bạn có thể đăng nhập ngay.";
            ViewBag.MessageType = "success";

            // Xóa form để tránh submit lại
            ModelState.Clear();

            return View();
        }
    }
}