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
            try
            {
                // ĐÃ SỬA: Bọc lỗi khi Identity.API sập
                var user = await _identityService.LoginAsync(loginDto);

                if (user != null)
                {
                    HttpContext.Session.SetObject("User", user);

                    if (!string.IsNullOrEmpty(user.Token))
                    {
                        HttpContext.Session.SetString("JWToken", user.Token);
                    }

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

                ViewBag.ErrorMessage = "Sai tài khoản hoặc mật khẩu.";
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Hệ thống xác thực đang bảo trì. Vui lòng thử lại sau!";
            }

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

            try
            {
                var user = await _identityService.LoginWithGoogleAsync(new GoogleLoginDto { IdToken = idToken });

                if (user != null)
                {
                    HttpContext.Session.SetObject("User", user);

                    if (!string.IsNullOrEmpty(user.Token))
                        HttpContext.Session.SetString("JWToken", user.Token);

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
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Hệ thống xác thực đang bảo trì. Vui lòng thử lại sau!";
            }

            ViewBag.GoogleClientId = _configuration["Google:ClientId"];
            return View("Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            try
            {
                var result = await _identityService.RegisterAsync(registerDto);

                if (result)
                {
                    TempData["SuccessMessage"] = "Đăng ký thành công! Mời bạn đăng nhập.";
                    return RedirectToAction("Login");
                }
                ViewBag.ErrorMessage = "Đăng ký thất bại. Tên tài khoản có thể đã tồn tại.";
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Hệ thống đăng ký đang tạm dừng. Vui lòng thử lại sau!";
            }

            return View(registerDto);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ResetPasswordDto resetPasswordDto)
        {
            try
            {
                var result = await _identityService.ResetPasswordAsync(resetPasswordDto);

                if (result)
                {
                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Mời bạn đăng nhập.";
                    return RedirectToAction("Login");
                }
                ViewBag.ErrorMessage = "Không tìm thấy tài khoản hoặc lỗi hệ thống.";
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Hệ thống khôi phục đang bảo trì. Vui lòng thử lại sau!";
            }

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
            try
            {
                var result = await _identityService.ResetPasswordAsync(resetPasswordDto);
                if (result)
                {
                    TempData["SuccessMessage"] = "Mật khẩu đã được đổi thành công. Hãy đăng nhập lại.";
                    return RedirectToAction("Login");
                }
                ViewBag.ErrorMessage = "Không thể đổi mật khẩu, vui lòng thử lại.";
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Máy chủ xác thực đang lỗi. Vui lòng thử lại sau!";
            }

            return View(resetPasswordDto);
        }

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
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> OrderHistory()
        {
            var userSession = HttpContext.Session.GetObject<UserDto>("User");
            if (userSession == null) return RedirectToAction("Login");

            try
            {
                // ĐÃ SỬA: Bọc lỗi khi lấy lịch sử mua hàng từ Order.API
                var orders = await _orderService.GetOrdersByUserIdAsync(userSession.Id);
                return View(orders);
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Hệ thống truy xuất đơn hàng đang bảo trì. Bạn không thể xem lịch sử lúc này.";
                return View(new List<OrderDto>());
            }
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            var user = HttpContext.Session.GetObject<UserDto>("User");
            if (user == null) return RedirectToAction("Login");

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(UserDto updatedUser)
        {
            var userSession = HttpContext.Session.GetObject<UserDto>("User");
            if (userSession == null) return RedirectToAction("Login");

            updatedUser.Id = userSession.Id;

            try
            {
                bool isUpdateSuccess = await _identityService.UpdateProfileAsync(updatedUser);

                if (isUpdateSuccess)
                {
                    userSession.FullName = updatedUser.FullName;
                    userSession.Email = updatedUser.Email;
                    HttpContext.Session.SetObject("User", userSession);

                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction(nameof(MyAccount));
                }
                ViewBag.ErrorMessage = "Không thể cập nhật thông tin (Email có thể đã tồn tại).";
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Máy chủ cập nhật đang bận. Vui lòng thử lại sau!";
            }

            return View(updatedUser);
        }
    }
}
