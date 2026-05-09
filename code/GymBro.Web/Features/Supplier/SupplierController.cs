using GymBro.Application.Supply;
using GymBro.Core;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class SupplierController : BaseAdminController
    {
        private readonly ISupplierAdminService _supplierAdminService;

        public SupplierController(ISupplierAdminService supplierAdminService)
        {
            _supplierAdminService = supplierAdminService;
        }

        public async Task<IActionResult> Index()
        {
            var suppliers = await _supplierAdminService.GetListAsync();
            return View(suppliers);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue)
            {
                return BadRequest();
            }

            var supplier = await _supplierAdminService.GetByIdAsync(id.Value);
            return supplier == null ? NotFound() : View(supplier);
        }

        public IActionResult Create()
        {
            return View(new Supplier
            {
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            var result = await _supplierAdminService.CreateAsync(new SupplierUpsertRequest
            {
                SupplierName = supplier.SupplierName,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                IsActive = supplier.IsActive
            });

            if (!result.Succeeded)
            {
                AddModelErrors(result.Errors);
                return View(supplier);
            }

            TempData["SuccessMessage"] = result.Message ?? "Đã tạo nhà cung cấp thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue)
            {
                return BadRequest();
            }

            var supplier = await _supplierAdminService.GetByIdAsync(id.Value);
            return supplier == null ? NotFound() : View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.Id)
            {
                return BadRequest();
            }

            var result = await _supplierAdminService.UpdateAsync(new SupplierUpsertRequest
            {
                Id = id,
                SupplierName = supplier.SupplierName,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                IsActive = supplier.IsActive
            });

            if (result.NotFound)
            {
                TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy nhà cung cấp cần cập nhật.";
                return RedirectToAction(nameof(Index));
            }

            if (!result.Succeeded)
            {
                AddModelErrors(result.Errors);
                return View(supplier);
            }

            TempData["SuccessMessage"] = result.Message ?? "Đã cập nhật nhà cung cấp thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!id.HasValue)
            {
                return BadRequest();
            }

            var supplier = await _supplierAdminService.GetByIdAsync(id.Value);
            return supplier == null ? NotFound() : View(supplier);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _supplierAdminService.DeleteAsync(id);

            if (result.NotFound)
            {
                TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy nhà cung cấp cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        private void AddModelErrors(IReadOnlyDictionary<string, string[]> errors)
        {
            foreach (var error in errors)
            {
                foreach (var message in error.Value)
                {
                    ModelState.AddModelError(error.Key, message);
                }
            }
        }
    }
}
