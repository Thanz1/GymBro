using GymBro.Core;
using GymBro.Infrastructure;
using GymBro.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly GymBroDbContext _context;

        public ReviewsController(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> GetByProduct(int productId)
        {
            var model = new ProductReviewsViewModel
            {
                ProductId = productId,
                DetailsUrl = Url.Action("Details", "Home", new { id = productId }) ?? $"/Home/Details/{productId}",
                IsAuthenticated = HttpContext.Session.GetObject<User>("User") != null,
                Reviews = await _context.Reviews
                    .Include(review => review.User)
                    .Where(review => review.ProductId == productId)
                    .OrderByDescending(review => review.CreatedDate)
                    .ToListAsync()
            };

            return PartialView("_ProductReviews", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int rating, string comment)
        {
            var user = HttpContext.Session.GetObject<User>("User");
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new
                {
                    returnUrl = Url.Action("Details", "Home", new { id = productId })
                });
            }

            var trimmedComment = string.IsNullOrWhiteSpace(comment) ? string.Empty : comment.Trim();
            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Số sao đánh giá không hợp lệ.";
                return RedirectToAction("Details", "Home", new { id = productId });
            }

            if (string.IsNullOrWhiteSpace(trimmedComment))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập nội dung đánh giá.";
                return RedirectToAction("Details", "Home", new { id = productId });
            }

            var productExists = await _context.Products.AnyAsync(product => product.Id == productId);
            if (!productExists)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Shop", "Home");
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = user.Id,
                Rating = rating,
                Comment = trimmedComment,
                CreatedDate = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá!";
            return RedirectToAction("Details", "Home", new { id = productId });
        }
    }
}
