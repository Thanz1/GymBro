using GymBro.Contracts;
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;
using GymBro.Web.Helpers;

namespace GymBro.Web.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // Lấy danh sách đánh giá theo sản phẩm (Dùng PartialView để load Ajax)
        public async Task<IActionResult> GetByProduct(int productId)
        {
            try
            {
                var reviews = await _reviewService.GetReviewsByProductIdAsync(productId);
                if (reviews == null) reviews = new List<ReviewDto>();
                var sortedReviews = reviews.OrderByDescending(r => r.CreatedDate).ToList();

                // THÊM DÒNG NÀY ĐỂ GẮN CHẶT ID SẢN PHẨM VÀO VIEW
                ViewBag.ProductId = productId;

                return PartialView("_ProductReviews", sortedReviews);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GYMBRO LỖI] Không thể lấy đánh giá: {ex.Message}");

                // Thêm cả ở phần bắt lỗi cho chắc chắn
                ViewBag.ProductId = productId;

                return PartialView("_ProductReviews", new List<ReviewDto>());
            }
        }

        // Thêm đánh giá mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int rating, string comment)
        {
            // Lấy UserDto từ Session (Đã thống nhất dùng UserDto ở các phần trước)
            var user = HttpContext.Session.GetObject<UserDto>("User");

            if (user == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để đánh giá.";
                return RedirectToAction("Login", "Account");
            }

            var reviewDto = new ReviewDto
            {
                ProductId = productId,
                UserId = user.Id,
                UserName = user.FullName, // Để hiển thị tên người đánh giá mà không cần Include User
                Rating = rating,
                Comment = comment,
                CreatedDate = DateTime.Now
            };

            var success = await _reviewService.AddReviewAsync(reviewDto);

            if (success)
            {
                TempData["SuccessMessage"] = "Cảm ơn đánh giá của bạn!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể gửi đánh giá lúc này.";
            }

            return RedirectToAction("Details", "Home", new { id = productId });
        }
    }
}