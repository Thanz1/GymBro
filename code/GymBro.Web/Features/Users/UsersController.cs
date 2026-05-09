using GymBro.Application.Users;
using GymBro.Core;
using GymBro.Web.Features.Users;
using GymBro.Web.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class UsersController : BaseAdminController
    {
        private const int PageSize = 10;

        private readonly IUserManagementService _userManagementService;

        public UsersController(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        public async Task<IActionResult> Index(int? page, string? searchString)
        {
            var result = await _userManagementService.GetPageAsync(new UserAdminPageQuery
            {
                PageNumber = page ?? 1,
                PageSize = PageSize,
                SearchString = searchString
            });

            return View(new UserManagementIndexViewModel
            {
                Items = result.Items,
                SearchString = result.SearchString,
                PageNumber = result.PageNumber,
                TotalPages = result.TotalPages,
                TotalItems = result.TotalItems
            });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue)
            {
                return BadRequest();
            }

            var user = await _userManagementService.GetByIdAsync(id.Value);
            return user == null ? NotFound() : View(user);
        }

        public IActionResult Create()
        {
            return View(new User
            {
                Role = "User",
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            var result = await _userManagementService.CreateAsync(new CreateManagedUserRequest
            {
                Username = user.Username,
                Password = user.Password,
                FullName = user.FullName,
                Email = user.Email,
                Address = user.Address,
                Role = user.Role,
                IsActive = user.IsActive
            });

            if (!result.Succeeded)
            {
                AddModelErrors(result.Errors);
                return View(user);
            }

            TempData["SuccessMessage"] = result.Message ?? "Đã tạo người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManagementService.GetByIdAsync(id);
            return user == null ? NotFound() : View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user, string? NewPassword)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            var currentSessionUser = HttpContext.Session.GetObject<User>("User");

            var result = await _userManagementService.UpdateAsync(new UpdateManagedUserRequest
            {
                Id = id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Address = user.Address,
                Role = user.Role,
                NewPassword = NewPassword,
                CurrentUserId = currentSessionUser?.Id
            });

            if (result.NotFound)
            {
                TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy người dùng cần cập nhật.";
                return RedirectToAction(nameof(Index));
            }

            if (!result.Succeeded)
            {
                AddModelErrors(result.Errors);
                return View(user);
            }

            if (currentSessionUser != null
                && result.User != null
                && currentSessionUser.Id == result.User.Id)
            {
                HttpContext.Session.SetObject("User", BuildSessionUser(result.User));
            }

            TempData["SuccessMessage"] = result.Message ?? "Đã cập nhật người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManagementService.GetByIdAsync(id);
            return user == null ? NotFound() : View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentSessionUser = HttpContext.Session.GetObject<User>("User");
            var result = await _userManagementService.DeleteAsync(id, currentSessionUser?.Id);

            if (result.NotFound)
            {
                TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy người dùng cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = result.Message ?? "Đã xóa người dùng thành công.";
            return RedirectToAction(nameof(Index));
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
