using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class OrdersController : BaseAdminController
    {
        private readonly GymBroDbContext _context;
        public OrdersController(GymBroDbContext context) { _context = context; }

        public async Task<IActionResult> Index(string searchString, string status)
        {
            var query = _context.Orders.Include(o => o.User).AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.TrangThai == status);

            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(o => o.Id.ToString() == searchString || o.User.Username.Contains(searchString));

            ViewBag.CurrentStatus = status;
            return View(await query.OrderByDescending(o => o.NgayDat).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            return order == null ? NotFound() : View(order);
        }

        // EDIT (Chủ yếu để cập nhật trạng thái đơn hàng)
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            return order == null ? NotFound() : View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Order order)
        {
            if (id != order.Id) return NotFound();

            var existingOrder = await _context.Orders.FindAsync(id);
            if (existingOrder != null)
            {
                existingOrder.TrangThai = order.TrangThai; // Cập nhật trạng thái
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}