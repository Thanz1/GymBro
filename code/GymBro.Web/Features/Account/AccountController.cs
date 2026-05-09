using GymBro.Application.Account;
using GymBro.Application.Auth;
using GymBro.Core;
using GymBro.Web.Features.Account;
using GymBro.Web.Features.Orders;
using GymBro.Web.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IUserAuthenticationService _authenticationService;

        public AccountController(
            IAccountService accountService,
            IUserAuthenticationService authenticationService)
        {
            _accountService = accountService;
            _authenticationService = authenticationService;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var result = await _authenticationService.RegisterAsync(new RegisterUserRequest
            {
                Username = user.Username,
                Password = user.Password,
                FullName = user.FullName,
                Email = user.Email,
                Address = user.Address,
                Role = "User",
                IsActive = true
            });

            if (!result.Succeeded)
            {
                AddModelErrors(result.Errors);
                return View(user);
            }

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : null;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            var safeReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : null;

            ViewBag.ReturnUrl = safeReturnUrl;

            var result = await _authenticationService.AuthenticateAsync(new LoginUserRequest
            {
                Username = username,
                Password = password
            });

            if (!result.Succeeded || result.User == null)
            {
                ViewBag.ErrorMessage = "Sai tên đăng nhập hoặc mật khẩu.";
                return View();
            }

            var user = result.User;
            if (!user.IsActive)
            {
                ViewBag.ErrorMessage = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ admin.";
                return View();
            }

            UpdateSessionUser(user);

            if (!string.IsNullOrWhiteSpace(safeReturnUrl))
            {
                return LocalRedirect(safeReturnUrl);
            }

            if (user.Role == "Admin")
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("User");
            HttpContext.Session.Remove("Cart");
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> MyAccount()
        {
            var currentUserId = GetCurrentSessionUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var overview = await _accountService.GetOverviewAsync(currentUserId.Value);
            if (overview == null)
            {
                HttpContext.Session.Remove("User");
                return RedirectToAction("Login");
            }

            UpdateSessionUser(overview.User);

            return View(new AccountDashboardViewModel
            {
                User = new AccountSummaryViewModel
                {
                    Username = overview.User.Username,
                    FullName = overview.User.FullName,
                    Email = overview.User.Email,
                    Address = overview.User.Address,
                    Role = overview.User.Role
                },
                RecentOrders = overview.RecentOrders
                    .Select(OrderPresentationFactory.CreateListItem)
                    .ToList()
            });
        }

        public async Task<IActionResult> EditProfile()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var model = new AccountProfileViewModel
            {
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Address = user.Address
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(AccountProfileViewModel model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _accountService.UpdateProfileAsync(user.Id, new UpdateAccountProfileRequest
            {
                Username = model.Username,
                FullName = model.FullName,
                Email = model.Email,
                Address = model.Address
            });

            if (result.NotFound)
            {
                HttpContext.Session.Remove("User");
                return RedirectToAction("Login");
            }

            if (!result.Succeeded)
            {
                AddModelErrors(result.Errors);
                return View(model);
            }

            UpdateSessionUser(result.User ?? user);

            TempData["SuccessMessage"] = result.Message ?? "Cập nhật thông tin tài khoản thành công.";
            return RedirectToAction(nameof(MyAccount));
        }

        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetObject<User>("User") == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _accountService.ChangePasswordAsync(user.Id, new ChangeAccountPasswordRequest
            {
                OldPassword = oldPassword,
                NewPassword = newPassword,
                ConfirmPassword = confirmPassword
            });

            if (result.NotFound)
            {
                HttpContext.Session.Remove("User");
                return RedirectToAction("Login");
            }

            if (!result.Succeeded)
            {
                ViewBag.ErrorMessage = result.Errors.Values.SelectMany(messages => messages).FirstOrDefault()
                    ?? result.Message
                    ?? "Đổi mật khẩu thất bại.";
                return View();
            }

            UpdateSessionUser(result.User ?? user);

            TempData["SuccessMessage"] = result.Message ?? "Đổi mật khẩu thành công!";
            return RedirectToAction("MyAccount");
        }

        public async Task<IActionResult> OrderHistory()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var orders = await _accountService.GetOrderHistoryAsync(user.Id);
            return View(new AccountOrderHistoryViewModel
            {
                Orders = orders
                    .Select(OrderPresentationFactory.CreateListItem)
                    .ToList()
            });
        }

        public async Task<IActionResult> OrderDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var details = await _accountService.GetOrderDetailsAsync(user.Id, id.Value);
            if (details == null)
            {
                return NotFound();
            }

            return View(new AccountOrderDetailsViewModel
            {
                Order = OrderPresentationFactory.CreateReadOnly(details.Order, details.LatestPayment)
            });
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var currentUserId = GetCurrentSessionUserId();
            if (!currentUserId.HasValue)
            {
                return null;
            }

            var user = await _accountService.GetActiveUserAsync(currentUserId.Value);
            if (user == null)
            {
                HttpContext.Session.Remove("User");
                return null;
            }

            return user;
        }

        private int? GetCurrentSessionUserId()
        {
            return HttpContext.Session.GetObject<User>("User")?.Id;
        }

        private void UpdateSessionUser(User user)
        {
            HttpContext.Session.SetObject("User", BuildSessionUser(user));
        }

        private void AddModelErrors(IReadOnlyDictionary<string, string[]> errors)
        {
            foreach (var error in errors)
            {
                foreach (var message in error.Value)
                {
                    ModelState.AddModelError(error.Key, message);
                }
            }
        }

        private static User BuildSessionUser(User user)
        {
            return new User
            {
                Id = user.Id,
                Username = user.Username,
                Password = string.Empty,
                FullName = user.FullName,
                Email = user.Email,
                Address = user.Address,
                Role = user.Role,
                CreatedDate = user.CreatedDate,
                IsActive = user.IsActive
            };
        }
    }
}
