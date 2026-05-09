using GymBro.Contracts;
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class UsersController : BaseAdminController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // 1. Danh sách người dùng
        public async Task<IActionResult> Index(string searchString)
        {
            var users = await _userService.GetAllUsersAsync(searchString);
            ViewBag.SearchString = searchString;
            return View(users);
        }

        // 2. Chi tiết
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            return user == null ? NotFound() : View(user);
        }

        // 3. Thêm mới (GET)
        public IActionResult Create() => View();

        // 4. Thêm mới (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserDto userDto)
        {
            if (ModelState.IsValid)
            {
                var result = await _userService.CreateUserAsync(userDto);
                // SỬA: IsSuccess -> Success
                if (result.Success) return RedirectToAction(nameof(Index));

                ModelState.AddModelError("", result.Message ?? "Lỗi khi tạo người dùng.");
            }
            return View(userDto);
        }

        // 5. Chỉnh sửa (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            return user == null ? NotFound() : View(user);
        }

        // 6. Chỉnh sửa (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserDto userDto, string? NewPassword)
        {
            if (id != userDto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var success = await _userService.UpdateUserAsync(id, userDto, NewPassword);
                if (success) return RedirectToAction(nameof(Index));

                ModelState.AddModelError("", "Cập nhật không thành công.");
            }
            return View(userDto);
        }

        // 7. Xóa
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            return user == null ? NotFound() : View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _userService.DeleteUserAsync(id);
            // SỬA: IsSuccess -> Success
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Xóa người dùng thành công.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
