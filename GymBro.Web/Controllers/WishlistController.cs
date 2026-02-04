using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebGymBro.Helpers;

namespace GymBro.Web.Controllers
{
    public class WishlistController : Controller
    {
        private readonly GymBroDbContext _context;

        public WishlistController(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = HttpContext.Session.GetObject<User>("User");
            if (user == null) return RedirectToAction("Login", "Account");

            var wishlist = await _context.Wishlists
                .Include(w => w.Product)
                .Where(w => w.UserId == user.Id)
                .OrderByDescending(w => w.NgayTao)
                .ToListAsync();

            return View(wishlist);
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var user = HttpContext.Session.GetObject<User>("User");
            if (user == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập!", requireLogin = true });
            }

            var existingItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == user.Id && w.ProductId == productId);

            if (existingItem == null)
            {
                _context.Wishlists.Add(new Wishlist { UserId = user.Id, ProductId = productId, NgayTao = DateTime.Now });
                await _context.SaveChangesAsync();
                return Json(new { success = true, isAdded = true, message = "Đã thích!" });
            }
            else
            {
                _context.Wishlists.Remove(existingItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isAdded = false, message = "Đã bỏ thích." });
            }
        }
    }
}