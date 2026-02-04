using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class PaymentsController : BaseAdminController
    {
        private readonly GymBroDbContext _context;
        public PaymentsController(GymBroDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Payments
                .Include(p => p.Order)
                .OrderByDescending(p => p.NgayThanhToan).ToListAsync());
        }
    }
}