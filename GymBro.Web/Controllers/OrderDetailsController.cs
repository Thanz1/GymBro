using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class OrderDetailsController : BaseAdminController
    {
        private readonly GymBroDbContext _context;
        public OrderDetailsController(GymBroDbContext context) { _context = context; }

        public async Task<IActionResult> Index(int orderId)
        {
            var details = _context.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.OrderId == orderId);
            return View(await details.ToListAsync());
        }
    }
}