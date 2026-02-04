using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class AdminReviewsController : BaseAdminController
    {
        private readonly GymBroDbContext _context;
        public AdminReviewsController(GymBroDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var reviews = _context.Reviews.Include(r => r.Product).Include(r => r.User);
            return View(await reviews.OrderByDescending(r => r.NgayTao).ToListAsync());
        }

        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null) { _context.Reviews.Remove(review); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }
    }
}