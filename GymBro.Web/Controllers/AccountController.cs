using System.Security.Claims;
using GymBro.Core;
using GymBro.Infrastructure;
using GymBro.Web.Helpers; // Đảm bảo bạn đã có class SessionExtensions
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly GymBroDbContext _context;

        public AccountController(GymBroDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. ĐĂNG KÝ
        // ==========================================
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng tên đăng nhập
                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                    return View(user);
                }

                // Kiểm tra trùng Email
                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                    return View(user);
                }

                // Mã hóa mật khẩu & thiết lập mặc định
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                user.Role = "User";
                user.CreatedDate = DateTime.Now;
                user.IsActive = true; // Kích hoạt ngay khi đăng ký

                // Lưu vào DB
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            return View(user);
        }

        // ==========================================
        // 2. ĐĂNG NHẬP
        // ==========================================
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user != null)
            {
                // Kiểm tra mật khẩu
                if (BCrypt.Net.BCrypt.Verify(password, user.Password))
                {
                    // KIỂM TRA TRẠNG THÁI (CS1061 FIX)
                    if (!user.IsActive)
                    {
                        ViewBag.ErrorMessage = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin.";
                        return View();
                    }

                    // Lưu thông tin vào Session
                    HttpContext.Session.SetObject("User", user);

                    // ĐIỀU HƯỚNG THÔNG MINH (FIX 404)
                    if (user.Role == "Admin")
                    {
                        // Sửa: Gọi đúng Action "Dashboard" trong "AdminController" vùng "Admin"
                        return RedirectToAction("Dashboard", "Admin");
                    }

                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.ErrorMessage = "Sai tên đăng nhập hoặc mật khẩu.";
            return View();
        }

        // ==========================================
        // 3. ĐĂNG XUẤT
        // ==========================================
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("User");
            HttpContext.Session.Remove("Cart");
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 4. TÀI KHOẢN CỦA TÔI
        // ==========================================
        public async Task<IActionResult> MyAccount()
        {
            var userSession = HttpContext.Session.GetObject<User>("User");
            if (userSession == null) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userSession.Id);
            if (user == null) return NotFound();

            // Lấy 5 đơn hàng mới nhất của người dùng này
            ViewBag.OrderHistory = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            return View(user);
        }

        // ==========================================
        // 5. ĐỔI MẬT KHẨU
        // ==========================================
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetObject<User>("User") == null)
                return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userSession = HttpContext.Session.GetObject<User>("User");
            if (userSession == null) return RedirectToAction("Login");

            if (newPassword != confirmPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            var user = await _context.Users.FindAsync(userSession.Id);
            if (user == null) return NotFound();

            // Kiểm tra mật khẩu cũ
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.Password))
            {
                ViewBag.ErrorMessage = "Mật khẩu cũ không chính xác.";
                return View();
            }

            // Hash mật khẩu mới
            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("MyAccount");
        }

        // ==========================================
        // 6. LỊCH SỬ ĐƠN HÀNG
        // ==========================================
        public async Task<IActionResult> OrderHistory()
        {
            var userSession = HttpContext.Session.GetObject<User>("User");
            if (userSession == null) return RedirectToAction("Login");

            var orders = await _context.Orders
                .Where(o => o.UserId == userSession.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ==========================================
        // 7. CHI TIẾT ĐƠN HÀNG
        // ==========================================
        public async Task<IActionResult> OrderDetails(int? id)
        {
            if (id == null) return NotFound();

            var userSession = HttpContext.Session.GetObject<User>("User");
            if (userSession == null) return RedirectToAction("Login");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userSession.Id);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}