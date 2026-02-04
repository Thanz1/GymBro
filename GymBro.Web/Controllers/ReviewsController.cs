using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using WebGymBro.Helpers;

namespace GymBro.Web.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly GymBroDbContext _context;

        public ReviewsController(GymBroDbContext context)
        {
            _context = context;
        }

        // Action này được gọi từ View Details (dùng ViewComponent sẽ tốt hơn nhưng giữ logic cũ cho bạn dễ hiểu)
        public IActionResult GetByProduct(int productId)
        {
            var reviews = _context.Reviews
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.NgayTao)
                .ToList();

            return PartialView("_ProductReviews", reviews);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int rating, string comment)
        {
            var user = HttpContext.Session.GetObject<User>("User");
            if (user == null) return RedirectToAction("Login", "Account");

            var review = new Review
            {
                ProductId = productId,
                UserId = user.Id,
                Rating = rating,
                Comment = comment,
                NgayTao = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn đánh giá của bạn!";
            return RedirectToAction("Details", "Home", new { id = productId });
        }
    }
}