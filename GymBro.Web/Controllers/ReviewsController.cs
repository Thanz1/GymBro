using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using GymBro.Web.Helpers;

namespace GymBro.Web.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly GymBroDbContext _context;

        public ReviewsController(GymBroDbContext context)
        {
            _context = context;
        }

        public IActionResult GetByProduct(int productId)
        {
            var reviews = _context.Reviews
                .Where(r => r.ProductId == productId)
                // SỬA: NgayTao -> CreatedDate
                .OrderByDescending(r => r.CreatedDate)
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
                // SỬA: NgayTao -> CreatedDate
                CreatedDate = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn đánh giá của bạn!";
            return RedirectToAction("Details", "Home", new { id = productId });
        }
    }
}