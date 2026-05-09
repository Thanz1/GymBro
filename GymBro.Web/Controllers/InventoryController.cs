using GymBro.Contracts;
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class InventoryController : BaseAdminController
    {
        private readonly IProductService _productService;

        public InventoryController(IProductService productService)
        {
            _productService = productService;
        }

        // 1. Danh sách tồn kho
        public async Task<IActionResult> Index(string searchString)
        {
            // Gọi API để lấy danh sách sản phẩm thay vì truy vấn DB
            var products = await _productService.GetAllProductsAsync();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.ProductName.Contains(searchString, StringComparison.OrdinalIgnoreCase));
                ViewBag.SearchString = searchString;
            }

            return View(products.OrderBy(p => p.ProductName).ToList());
        }

        // 2. Giao diện điều chỉnh kho (GET)
        public async Task<IActionResult> Adjust(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            return product == null ? NotFound() : View(product);
        }

        // 3. Xử lý điều chỉnh kho (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(int id, int NewQuantity, string Note)
        {
            // Đẩy trách nhiệm tính toán Diff và lưu Transaction sang API
            var success = await _productService.AdjustStockAsync(id, NewQuantity, Note);

            if (success)
            {
                TempData["SuccessMessage"] = "Cập nhật kho thành công!";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Không thể cập nhật kho. Vui lòng thử lại.");
            var product = await _productService.GetProductByIdAsync(id);
            return View(product);
        }
    }
}