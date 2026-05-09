using GymBro.Contracts; // Dùng duy nhất namespace này cho DTO
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class AdminSetupController : Controller
    {
        private readonly IIdentityService _identityService;

        public AdminSetupController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        [HttpGet]
        public async Task<IActionResult> CreateAdmin()
        {
            // Kiểm tra qua API xem hệ thống đã có Admin chưa
            if (await _identityService.HasAdminAsync())
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(RegisterDto adminDto, string confirmPassword)
        {
            if (adminDto.Password != confirmPassword)
            {
                ViewBag.Message = "Mật khẩu xác nhận không khớp.";
                ViewBag.MessageType = "danger";
                return View(adminDto);
            }

            // Đẩy trách nhiệm tạo Admin sang Identity.API (Port 7001)
            var result = await _identityService.CreateAdminAsync(adminDto);

            if (result)
            {
                ViewBag.Message = "Tạo tài khoản Admin thành công!";
                ViewBag.MessageType = "success";
                ModelState.Clear();
            }
            else
            {
                ViewBag.Message = "Hệ thống đã có Admin hoặc có lỗi xảy ra.";
                ViewBag.MessageType = "danger";
            }

            return View();
        }
    }
}