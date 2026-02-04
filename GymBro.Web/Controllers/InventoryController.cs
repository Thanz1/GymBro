using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class InventoryController : BaseAdminController
    {
        private readonly GymBroDbContext _context;
        public InventoryController(GymBroDbContext context) { _context = context; }

        // Danh sách tồn kho
        public async Task<IActionResult> Index(string searchString)
        {
            var products = _context.Products.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
                products = products.Where(p => p.TenSanPham.Contains(searchString));

            return View(await products.OrderBy(p => p.TenSanPham).ToListAsync());
        }

        // Điều chỉnh kho thủ công (Nhập/Xuất)
        public async Task<IActionResult> Adjust(int id)
        {
            var product = await _context.Products.FindAsync(id);
            return product == null ? NotFound() : View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Adjust(int id, int NewQuantity, string Note)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            int diff = NewQuantity - product.SoLuongTon;
            product.SoLuongTon = NewQuantity;

            // Ghi log giao dịch kho (Nếu bạn có bảng InventoryTransaction)
            /*
            _context.InventoryTransactions.Add(new InventoryTransaction {
                ProductId = id, QuantityChange = diff, Note = Note, Date = DateTime.Now 
            });
            */

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}