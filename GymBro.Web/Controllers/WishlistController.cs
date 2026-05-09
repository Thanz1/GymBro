using GymBro.Contracts;
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;
using GymBro.Web.Helpers;

namespace GymBro.Web.Controllers
{
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        // GET: Hiển thị danh sách sản phẩm yêu thích
        public async Task<IActionResult> Index()
        {
            // Lấy UserDto từ Session
            var user = HttpContext.Session.GetObject<UserDto>("User");
            if (user == null) return RedirectToAction("Login", "Account");

            var wishlist = await _wishlistService.GetWishlistByUserIdAsync(user.Id);

            // Sắp xếp theo ngày tạo mới nhất
            return View(wishlist.OrderByDescending(w => w.CreatedDate).ToList());
        }

        // POST: Thêm/Xóa sản phẩm khỏi danh sách yêu thích
        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var user = HttpContext.Session.GetObject<UserDto>("User");
            if (user == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập!", requireLogin = true });
            }

            // Đẩy logic xử lý (Check tồn tại -> Thêm/Xóa) sang API
            var isAdded = await _wishlistService.ToggleWishlistAsync(user.Id, productId);

            return Json(new
            {
                success = true,
                isAdded = isAdded,
                message = isAdded ? "Đã thêm vào danh sách yêu thích!" : "Đã xóa khỏi danh sách yêu thích."
            });
        }
    }
}