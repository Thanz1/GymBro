using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class AdminReviewsController : BaseAdminController
    {
        private readonly GymBroDbContext _context;

        public AdminReviewsController(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? page, string? searchString)
        {
            const int pageSize = 10;

            var query = _context.Reviews
                .Include(review => review.Product)
                .Include(review => review.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var keyword = searchString.Trim();
                query = query.Where(review =>
                    (review.Product != null && review.Product.ProductName.Contains(keyword))
                    || (review.User != null && review.User.Username.Contains(keyword))
                    || review.Comment.Contains(keyword));
            }

            query = query.OrderByDescending(review => review.CreatedDate);

            var totalItems = await query.CountAsync();
            var pagination = SetupPagination(page, totalItems, pageSize);
            var reviews = await query
                .Skip(pagination.SkipCount)
                .Take(pagination.PageSize)
                .ToListAsync();

            ViewBag.CurrentSearch = searchString;

            return View(reviews);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(item => item.Product)
                .Include(item => item.User)
                .FirstOrDefaultAsync(item => item.Id == id.Value);

            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                TempData["WarningMessage"] = "Đánh giá này không còn tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa đánh giá thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
