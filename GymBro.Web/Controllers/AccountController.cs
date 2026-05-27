using GymBro.Contracts;
using GymBro.Contracts.DTOs;
using GymBro.Service;
using GymBro.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace GymBro.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IIdentityService _identityService;
        private readonly IOrderService _orderService;
        private readonly IConfiguration _configuration;

        public AccountController(
            IIdentityService identityService,
            IOrderService orderService,
            IConfiguration configuration)
        {
            _identityService = identityService;
            _orderService = orderService;
            _configuration = configuration;
        }

        // =========================================================
        // 1. CHỨC NĂNG ĐĂNG NHẬP (LOGIN)
        // =========================================================

        [HttpGet]
        public IActionResult Login()
        {
            ViewBag.GoogleClientId = _configuration["Google:ClientId"];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var user = await _identityService.LoginAsync(loginDto);

            if (user != null)
            {
                // 1.1. Lưu thông tin vào Session để dùng cho toàn hệ thống Web
                HttpContext.Session.SetObject("User", user);

                if (!string.IsNullOrEmpty(user.Token))
                {
                    HttpContext.Session.SetString("JWToken", user.Token);
                }

                // 1.2. Đồng bộ danh tính với hệ thống bảo mật Cookie của ASP.NET Core
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username ?? "User"),
                    new Claim(ClaimTypes.Role, user.Role ?? "Customer")
                };
                var claimsIdentity = new ClaimsIdentity(claims, "Cookies"); // "Cookies" khớp với cài đặt trong Program.cs
                await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity));

                // 1.3. Điều hướng phân quyền sau đăng nhập
                if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Index", "Products");

                return RedirectToAction("Index", "Home");
            }

            ViewBag.ErrorMessage = "Sai tài khoản hoặc mật khẩu.";
            ViewBag.GoogleClientId = _configuration["Google:ClientId"];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoogleLogin(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                ViewBag.ErrorMessage = "Đăng nhập Google thất bại. Vui lòng thử lại.";
                ViewBag.GoogleClientId = _configuration["Google:ClientId"];
                return View("Login");
            }

            var user = await _identityService.LoginWithGoogleAsync(new GoogleLoginDto { IdToken = idToken });

            if (user != null)
            {
                // 2.1. Lưu Session như đăng nhập thường
                HttpContext.Session.SetObject("User", user);

                if (!string.IsNullOrEmpty(user.Token))
                    HttpContext.Session.SetString("JWToken", user.Token);

                // 2.2. Kích hoạt Cookie xác thực bảo mật cho tài khoản Google
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username ?? "User"),
                    new Claim(ClaimTypes.Role, user.Role ?? "Customer")
                };
                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity));

                if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Index", "Products");

                return RedirectToAction("Index", "Home");
            }

            ViewBag.ErrorMessage = "Đăng nhập Google thất bại. Kiểm tra cấu hình ClientId hoặc thử lại.";
            ViewBag.GoogleClientId = _configuration["Google:ClientId"];
            return View("Login");
        }

        // =========================================================
        // 2. CHỨC NĂNG ĐĂNG KÝ TÀI KHOẢN
        // =========================================================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            var result = await _identityService.RegisterAsync(registerDto);

            if (result)
            {
                TempData["SuccessMessage"] = "Đăng ký thành công! Mời bạn đăng nhập.";
                return RedirectToAction("Login");
            }

            ViewBag.ErrorMessage = "Đăng ký thất bại. Tên tài khoản có thể đã tồn tại.";
            return View(registerDto);
        }

        // =========================================================
        // 3. CHỨC NĂNG QUÊN & ĐỔI MẬT KHẨU
        // =========================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ResetPasswordDto resetPasswordDto)
        {
            var result = await _identityService.ResetPasswordAsync(resetPasswordDto);

            if (result)
            {
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Mời bạn đăng nhập.";
                return RedirectToAction("Login");
            }

            ViewBag.ErrorMessage = "Không tìm thấy tài khoản hoặc lỗi hệ thống.";
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string identifier)
        {
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

        // =========================================================
        // 4. CHỨC NĂNG ĐĂNG XUẤT & LỊCH SỬ ĐƠN HÀNG
        // =========================================================
        [HttpGet]
        public IActionResult MyAccount()
        {
            var user = HttpContext.Session.GetObject<UserDto>("User");
            if (user == null)
                return RedirectToAction("Login", "Account");

            return View(user);
        }

        public async Task<IActionResult> Logout()
        {
            // Xóa Session
            HttpContext.Session.Clear();

            // Hủy Cookie bảo mật lưu trong trình duyệt tránh lỗi nhảy trang
            await HttpContext.SignOutAsync("Cookies");

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
        public IActionResult EditProfile()
        {
            var user = HttpContext.Session.GetObject<UserDto>("User");
            if (user == null) return RedirectToAction("Login");

            return View(user); // Truyền dữ liệu cũ sang giao diện
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(UserDto updatedUser)
        {
            var userSession = HttpContext.Session.GetObject<UserDto>("User");
            if (userSession == null) return RedirectToAction("Login");

            // Ép ID để đảm bảo không bị cập nhật nhầm người khác
            updatedUser.Id = userSession.Id;

            // Gọi API thật
            bool isUpdateSuccess = await _identityService.UpdateProfileAsync(updatedUser);

            if (isUpdateSuccess)
            {
                // Cập nhật Session
                userSession.FullName = updatedUser.FullName;
                userSession.Email = updatedUser.Email;
                HttpContext.Session.SetObject("User", userSession);

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction(nameof(MyAccount));
            }

            ViewBag.ErrorMessage = "Không thể cập nhật thông tin (Email có thể đã tồn tại).";
            return View(updatedUser);
        }
    }
}
