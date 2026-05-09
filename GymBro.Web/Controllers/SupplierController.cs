using GymBro.Contracts;
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class SupplierController : BaseAdminController
    {
        private readonly ISupplierService _supplierService;

        public SupplierController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        // 1. Danh sách nhà cung cấp
        public async Task<IActionResult> Index()
        {
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            return View(suppliers);
        }

        // 2. Trang thêm mới
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierDto supplierDto)
        {
            if (ModelState.IsValid)
            {
                var success = await _supplierService.CreateSupplierAsync(supplierDto);
                if (success)
                {
                    TempData["SuccessMessage"] = "Thêm nhà cung cấp thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(supplierDto);
        }

        // 3. Trang sửa
        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SupplierDto supplierDto)
        {
            if (id != supplierDto.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                var success = await _supplierService.UpdateSupplierAsync(id, supplierDto);
                if (success)
                {
                    TempData["SuccessMessage"] = "Cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(supplierDto);
        }

        // 4. Xóa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _supplierService.DeleteSupplierAsync(id);
            if (success) TempData["SuccessMessage"] = "Đã xóa nhà cung cấp!";
            return RedirectToAction(nameof(Index));
        }
    }
}