<<<<<<< HEAD
﻿using System.Security.Claims;
using GymBro.Core;
using GymBro.Infrastructure;
using GymBro.Web.Helpers; // Đảm bảo bạn đã có class SessionExtensions ở đây
=======
﻿using GymBro.Core;
using GymBro.Infrastructure;
using GymBro.Web.Helpers; // Để dùng SessionExtensions
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
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

<<<<<<< HEAD
        // ==========================================
        // 1. ĐĂNG KÝ
        // ==========================================
=======
        // 1. ĐĂNG KÝ
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
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

<<<<<<< HEAD
                // Mã hóa mật khẩu
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                user.Role = "User"; // Mặc định là User thường
                user.CreatedDate = DateTime.Now; // Ghi lại ngày tạo

                // Lưu vào DB
=======
                // Mã hóa mật khẩu và tạo User
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                user.Role = "User"; // Mặc định là User thường

>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            return View(user);
        }

<<<<<<< HEAD
        // ==========================================
        // 2. ĐĂNG NHẬP
        // ==========================================
=======
        // 2. ĐĂNG NHẬP
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
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
<<<<<<< HEAD
                    // (Đã xóa đoạn kiểm tra IsActive để tránh lỗi Model)

                    // Lưu thông tin vào Session (Đăng nhập thành công)
                    HttpContext.Session.SetObject("User", user);

                    // Điều hướng thông minh: Admin về Dashboard, User về Trang chủ
                    if (user.Role == "Admin")
                    {
                        // Giả sử bạn có Area Admin, nếu không thì sửa lại dòng dưới
                        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    }

=======
                    // Lưu thông tin vào Session (Đăng nhập thành công)
                    HttpContext.Session.SetObject("User", user);
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.ErrorMessage = "Sai tên đăng nhập hoặc mật khẩu.";
            return View();
        }

<<<<<<< HEAD
        // ==========================================
        // 3. ĐĂNG XUẤT
        // ==========================================
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("User");
            HttpContext.Session.Remove("Cart"); // Xóa giỏ hàng khi đăng xuất cho an toàn
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 4. TÀI KHOẢN CỦA TÔI
        // ==========================================
=======
        // 3. ĐĂNG XUẤT
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("User");
            return RedirectToAction("Index", "Home");
        }

        // 4. TÀI KHOẢN CỦA TÔI
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
        public async Task<IActionResult> MyAccount()
        {
            var userSession = HttpContext.Session.GetObject<User>("User");
            if (userSession == null) return RedirectToAction("Login");

<<<<<<< HEAD
            // Lấy thông tin mới nhất từ DB
            var user = await _context.Users.FindAsync(userSession.Id);

            // Lấy 5 đơn hàng mới nhất
            ViewBag.OrderHistory = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
=======
            // Lấy thông tin mới nhất từ DB (để tránh Session bị cũ)
            var user = await _context.Users.FindAsync(userSession.Id);

            // Lấy lịch sử đơn hàng
            ViewBag.OrderHistory = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate) // Đã sửa NgayDat -> OrderDate
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
                .ToListAsync();

            return View(user);
        }

<<<<<<< HEAD
        // ==========================================
        // 5. ĐỔI MẬT KHẨU
        // ==========================================
=======
        // 5. ĐỔI MẬT KHẨU
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
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
<<<<<<< HEAD

            // Cập nhật DB
            _context.Users.Update(user);
=======
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("MyAccount");
        }

<<<<<<< HEAD
        // ==========================================
        // 6. LỊCH SỬ ĐƠN HÀNG (Xem tất cả)
        // ==========================================
=======
        // 6. LỊCH SỬ ĐƠN HÀNG (Xem tất cả)
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
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
<<<<<<< HEAD

        // ==========================================
        // 7. CHI TIẾT ĐƠN HÀNG (ĐÃ SỬA LỖI CRASH)
        // ==========================================
        public async Task<IActionResult> OrderDetails(int? id)
        {
            if (id == null) return NotFound();

            // SỬA QUAN TRỌNG: Lấy từ Session thay vì User.FindFirstValue (vì bạn đang dùng Session Authentication)
            var userSession = HttpContext.Session.GetObject<User>("User");
            if (userSession == null) return RedirectToAction("Login");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product) // Load sản phẩm để hiển thị ảnh/tên
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userSession.Id); // Chỉ cho xem đơn của chính mình

            if (order == null) return NotFound();

            return View(order);
        }
=======
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
    }
}