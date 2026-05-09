using GymBro.Core;
using GymBro.Infrastructure;
using GymBro.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var wishlist = await _context.Wishlists
                .Include(item => item.Product)
                    .ThenInclude(product => product!.Category)
                .Where(item => item.UserId == user.Id)
                .OrderByDescending(item => item.NgayTao)
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
                .FirstOrDefaultAsync(item => item.UserId == user.Id && item.ProductId == productId);

            if (existingItem == null)
            {
                _context.Wishlists.Add(new Wishlist { UserId = user.Id, ProductId = productId, NgayTao = DateTime.Now });
                await _context.SaveChangesAsync();
                return Json(new { success = true, isAdded = true, message = "Đã thích!" });
            }

            _context.Wishlists.Remove(existingItem);
            await _context.SaveChangesAsync();
            return Json(new { success = true, isAdded = false, message = "Đã bỏ thích." });
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId)
        {
            var user = HttpContext.Session.GetObject<User>("User");
            if (user == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để lưu yêu thích.", requireLogin = true });
            }

            var exists = await _context.Wishlists
                .AnyAsync(item => item.UserId == user.Id && item.ProductId == productId);

            if (!exists)
            {
                _context.Wishlists.Add(new Wishlist
                {
                    UserId = user.Id,
                    ProductId = productId,
                    NgayTao = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Đã thêm vào yêu thích!" });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int productId)
        {
            var user = HttpContext.Session.GetObject<User>("User");
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var item = await _context.Wishlists
                .FirstOrDefaultAsync(wishlist => wishlist.UserId == user.Id && wishlist.ProductId == productId);

            if (item == null)
            {
                TempData["WarningMessage"] = "Sản phẩm không còn trong danh sách yêu thích.";
                return RedirectToAction(nameof(Index));
            }

            _context.Wishlists.Remove(item);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi danh sách yêu thích.";
            return RedirectToAction(nameof(Index));
        }
    }
}
