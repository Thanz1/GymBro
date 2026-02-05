using GymBro.Core;
using GymBro.Infrastructure;
using GymBro.Web.Helpers; // Để dùng SessionExtensions
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

        // 1. ĐĂNG KÝ
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

                // Mã hóa mật khẩu và tạo User
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                user.Role = "User"; // Mặc định là User thường

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            return View(user);
        }

        // 2. ĐĂNG NHẬP
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Tìm user trong DB
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user != null)
            {
                // Kiểm tra mật khẩu (So sánh hash)
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);

                if (isPasswordValid)
                {
                    // Lưu thông tin vào Session (Đăng nhập thành công)
                    HttpContext.Session.SetObject("User", user);
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.ErrorMessage = "Sai tên đăng nhập hoặc mật khẩu.";
            return View();
        }

        // 3. ĐĂNG XUẤT
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("User");
            return RedirectToAction("Index", "Home");
        }

        // 4. TÀI KHOẢN CỦA TÔI
        public async Task<IActionResult> MyAccount()
        {
            var userSession = HttpContext.Session.GetObject<User>("User");
            if (userSession == null) return RedirectToAction("Login");

            // Lấy thông tin mới nhất từ DB (để tránh Session bị cũ)
            var user = await _context.Users.FindAsync(userSession.Id);

            // Lấy lịch sử đơn hàng
            ViewBag.OrderHistory = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate) // Đã sửa NgayDat -> OrderDate
                .ToListAsync();

            return View(user);
        }

        // 5. ĐỔI MẬT KHẨU
        public IActionResult ChangePassword()
        {
            var user = HttpContext.Session.GetObject<User>("User");
            if (user == null) return RedirectToAction("Login");
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

            // Kiểm tra mật khẩu cũ
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.Password))
            {
                ViewBag.ErrorMessage = "Mật khẩu cũ không đúng.";
                return View();
            }

            // Cập nhật mật khẩu mới
            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("MyAccount");
        }

        // 6. LỊCH SỬ ĐƠN HÀNG (Xem tất cả)
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
    }
}