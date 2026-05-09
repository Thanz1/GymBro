using GymBro.Application.Supply;
using GymBro.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Features.PurchaseOrders
{
    public class PurchaseOrdersController : BaseAdminController
    {
        private readonly IPurchaseOrderAdminService _purchaseOrderAdminService;

        public PurchaseOrdersController(IPurchaseOrderAdminService purchaseOrderAdminService)
        {
            _purchaseOrderAdminService = purchaseOrderAdminService;
        }

        public async Task<IActionResult> Index()
        {
            var purchaseOrders = await _purchaseOrderAdminService.GetListAsync();
            return View(purchaseOrders);
        }

        public async Task<IActionResult> Create()
        {
            var data = await _purchaseOrderAdminService.GetCreateDataAsync();
            return View(new PurchaseOrderCreateViewModel
            {
                SupplierId = data.SupplierId,
                OrderDate = data.OrderDate,
                SupplierOptions = data.SupplierOptions
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderCreateViewModel model)
        {
            var request = new PurchaseOrderCreateRequest
            {
                SupplierId = model.SupplierId,
                OrderDate = model.OrderDate
            };

            var result = await _purchaseOrderAdminService.CreateAsync(request);
            if (!result.Succeeded)
            {
                AddModelErrors(result.Errors);

                var data = await _purchaseOrderAdminService.GetCreateDataAsync(request);
                return View(new PurchaseOrderCreateViewModel
                {
                    SupplierId = data.SupplierId,
                    OrderDate = data.OrderDate,
                    SupplierOptions = data.SupplierOptions
                });
            }

            return RedirectToAction(nameof(Edit), new { id = result.PurchaseOrder?.Id });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            var data = await _purchaseOrderAdminService.GetEditDataAsync(id.Value);
            if (data == null)
            {
                return NotFound();
            }

            return View(new PurchaseOrderEditViewModel
            {
                PurchaseOrder = data.PurchaseOrder,
                ProductOptions = data.ProductOptions
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDetail(int purchaseOrderId, int productId, int quantity, decimal unitPrice)
        {
            var result = await _purchaseOrderAdminService.AddDetailAsync(new PurchaseOrderDetailRequest
            {
                PurchaseOrderId = purchaseOrderId,
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = unitPrice
            });

            if (!result.Succeeded)
            {
                TempData["Error"] = result.Message;
            }

            if (result.NotFound && !result.PurchaseOrderId.HasValue)
            {
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Edit), new { id = result.PurchaseOrderId ?? purchaseOrderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDetail(int id)
        {
            var result = await _purchaseOrderAdminService.DeleteDetailAsync(id);

            if (!result.Succeeded)
            {
                TempData["Error"] = result.Message;
            }

            if (!result.PurchaseOrderId.HasValue)
            {
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Edit), new { id = result.PurchaseOrderId.Value });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            var purchaseOrder = await _purchaseOrderAdminService.GetDeleteDataAsync(id.Value);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            return View(purchaseOrder);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _purchaseOrderAdminService.DeleteAsync(id);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _purchaseOrderAdminService.ApproveAsync(id);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;

            if (result.NotFound)
            {
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Edit), new { id = result.PurchaseOrderId ?? id });
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
