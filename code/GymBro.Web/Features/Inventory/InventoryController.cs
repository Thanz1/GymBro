using GymBro.Application.Supply;
using GymBro.Web.Features.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class InventoryController : BaseAdminController
    {
        private readonly IInventoryAdminService _inventoryAdminService;

        public InventoryController(IInventoryAdminService inventoryAdminService)
        {
            _inventoryAdminService = inventoryAdminService;
        }

        public async Task<IActionResult> Index(string? searchString)
        {
            var result = await _inventoryAdminService.GetOverviewAsync(searchString);
            return View(new InventoryOverviewViewModel
            {
                Products = result.Products,
                SearchString = result.SearchString
            });
        }

        public async Task<IActionResult> History(int? productId)
        {
            var result = await _inventoryAdminService.GetHistoryAsync(productId);
            return View(new InventoryHistoryViewModel
            {
                Transactions = result.Transactions,
                ProductName = result.ProductName,
                ProductId = result.ProductId
            });
        }

        public async Task<IActionResult> Adjust(int id)
        {
            var data = await _inventoryAdminService.GetAdjustDataAsync(id);
            if (data == null)
            {
                return NotFound();
            }

            return View(new InventoryAdjustViewModel
            {
                Product = data.Product,
                NewQuantity = data.NewQuantity,
                Note = data.Note
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(int id, int NewQuantity, string? Note)
        {
            var result = await _inventoryAdminService.AdjustAsync(new InventoryAdjustRequest
            {
                ProductId = id,
                NewQuantity = NewQuantity,
                Note = Note
            });

            if (result.NotFound)
            {
                return NotFound();
            }

            if (!result.Succeeded)
            {
                AddModelErrors(result.Errors);

                var data = result.Data ?? await _inventoryAdminService.GetAdjustDataAsync(id, NewQuantity, Note);
                if (data == null)
                {
                    return NotFound();
                }

                return View(new InventoryAdjustViewModel
                {
                    Product = data.Product,
                    NewQuantity = data.NewQuantity,
                    Note = data.Note
                });
            }

            TempData["SuccessMessage"] = result.Message;
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
