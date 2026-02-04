using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebGymBro.Helpers;

namespace GymBro.Web.Controllers
{
    public class UsersController : BaseAdminController
    {
        private readonly GymBroDbContext _context;
        public UsersController(GymBroDbContext context) { _context = context; }

        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.Username.Contains(searchString) || u.Email.Contains(searchString));
            }
            return View(await query.OrderByDescending(u => u.Id).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users.FindAsync(id);
            return user == null ? NotFound() : View(user);
        }

        // CREATE
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(User user)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                    return View(user);
                }

                user.Password = PasswordHelper.HashPassword(user.Password); // Mã hóa pass
                user.NgayTao = DateTime.Now;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // EDIT
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            return user == null ? NotFound() : View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, User user, string? NewPassword)
        {
            if (id != user.Id) return NotFound();

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null) return NotFound();

            // Cập nhật thông tin cơ bản
            existingUser.FullName = user.FullName;
            existingUser.Email = user.Email;
            existingUser.Address = user.Address;
            existingUser.Role = user.Role;

            // Nếu có nhập mật khẩu mới thì mới đổi
            if (!string.IsNullOrEmpty(NewPassword))
            {
                existingUser.Password = PasswordHelper.HashPassword(NewPassword);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // DELETE
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            return user == null ? NotFound() : View(user);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // Kiểm tra ràng buộc khóa ngoại (nếu user đã có đơn hàng thì không xóa)
                if (await _context.Orders.AnyAsync(o => o.UserId == id))
                {
                    TempData["ErrorMessage"] = "Không thể xóa user này vì đã có đơn hàng!";
                    return RedirectToAction(nameof(Index));
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}