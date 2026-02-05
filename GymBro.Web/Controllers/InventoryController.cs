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
            var products = _context.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
                // SỬA: TenSanPham -> ProductName
                products = products.Where(p => p.ProductName.Contains(searchString));

            // SỬA: TenSanPham -> ProductName
            return View(await products.OrderBy(p => p.ProductName).ToListAsync());
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

            // SỬA: SoLuongTon -> StockQuantity
            int diff = NewQuantity - product.StockQuantity;
            product.StockQuantity = NewQuantity;

            // ĐÃ MỞ COMMENT VÀ SỬA TÊN BIẾN CHO KHỚP MODEL
            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = id,              // Đã sửa ProductID -> ProductId (nếu cần)
                QuantityChange = diff,
                Note = Note ?? "Kiểm kê kho",
                CreatedDate = DateTime.Now,  // Đã sửa Date -> CreatedDate
                TransactionType = "Điều chỉnh"
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}