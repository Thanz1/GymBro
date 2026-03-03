using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GymBro.Core;
using GymBro.Infrastructure;

namespace GymBro.Web.Controllers
{
    public class OrdersController : BaseAdminController
    {
        private readonly GymBroDbContext _context;

        public OrdersController(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string status)
        {
            var query = _context.Orders.Include(o => o.User).AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var statusList = GetStatusList();
            ViewBag.Status = new SelectList(statusList, status);

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(x => x.Id == id);
            if (order == null) return NotFound();

            ViewBag.Status = new SelectList(GetStatusList(), order.Status);

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Order order)
        {
            if (id != order.Id) return NotFound();

            var existingOrder = await _context.Orders.FindAsync(id);
            if (existingOrder == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    existingOrder.Status = order.Status;

                    _context.Update(existingOrder);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Status = new SelectList(GetStatusList(), order.Status);
            return View(order);
        }

        private List<string> GetStatusList()
        {
            return new List<string>
            {
                "Chờ xử lý",
                "Đang xử lý",
                "Đang giao",
                "Đã giao",
                "Đã hủy"
            };
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}