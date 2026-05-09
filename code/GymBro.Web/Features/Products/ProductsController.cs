using GymBro.Application.Catalog;
using GymBro.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymBro.Web.Controllers
{
    public class ProductsController : BaseAdminController
    {
        private const int PageSize = 10;

        private readonly IProductAdminService _productAdminService;

        public ProductsController(IProductAdminService productAdminService)
        {
            _productAdminService = productAdminService;
        }

        public async Task<IActionResult> Index(int? page, string? searchString, int? categoryId)
        {
            var result = await _productAdminService.GetPageAsync(new ProductAdminPageQuery
            {
                PageNumber = page ?? 1,
                PageSize = PageSize,
                SearchString = searchString,
                CategoryId = categoryId
            });

            PopulatePagination(result);
            PopulateCategorySelectList(result.Categories, categoryId);

            ViewBag.CurrentSearch = result.SearchString;
            ViewBag.CurrentCategory = result.CategoryId;

            return View(result.Items);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue)
            {
                return BadRequest();
            }

            var product = await _productAdminService.GetDetailsAsync(id.Value);
            return product == null ? NotFound() : View(product);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateCategorySelectListAsync();
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            var result = await _productAdminService.CreateAsync(
                await BuildUpsertRequestAsync(product, imageFile));

            if (!result.Succeeded)
            {
                ApplyErrors(result);
                await PopulateCategorySelectListAsync(product.CategoryId);
                return View(product);
            }

            TempData["SuccessMessage"] = "ÄÃ£ táº¡o sáº£n pháº©m thÃ nh cÃ´ng.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id, int? page)
        {
            if (!id.HasValue)
            {
                return BadRequest();
            }

            var product = await _productAdminService.GetEditModelAsync(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            await PopulateCategorySelectListAsync(product.CategoryId);
            ViewBag.Page = page;

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile, int? page)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            var result = await _productAdminService.UpdateAsync(
                await BuildUpsertRequestAsync(product, imageFile));

            if (result.NotFound)
            {
                TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y sáº£n pháº©m cáº§n cáº­p nháº­t.";
                return RedirectToAction(nameof(Index), new { page });
            }

            if (!result.Succeeded)
            {
                ApplyErrors(result);
                await PopulateCategorySelectListAsync(product.CategoryId);
                ViewBag.Page = page;
                return View(product);
            }

            TempData["SuccessMessage"] = "ÄÃ£ cáº­p nháº­t sáº£n pháº©m thÃ nh cÃ´ng.";
            return RedirectToAction(nameof(Index), new { page });
        }

        public async Task<IActionResult> Delete(int? id, int? page)
        {
            if (!id.HasValue)
            {
                return BadRequest();
            }

            var product = await _productAdminService.GetDetailsAsync(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Page = page;
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int? page)
        {
            var result = await _productAdminService.DeleteAsync(id);
            if (result.NotFound)
            {
                TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y sáº£n pháº©m cáº§n xÃ³a.";
                return RedirectToAction(nameof(Index), new { page });
            }

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Index), new { page });
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { page });
        }

        private async Task PopulateCategorySelectListAsync(int? selectedCategoryId = null)
        {
            var categories = await _productAdminService.GetCategoryOptionsAsync();
            PopulateCategorySelectList(categories, selectedCategoryId);
        }

        private void PopulateCategorySelectList(
            IReadOnlyList<CategoryOptionDto> categories,
            int? selectedCategoryId = null)
        {
            ViewBag.Categories = new SelectList(
                categories,
                nameof(CategoryOptionDto.Id),
                nameof(CategoryOptionDto.Name),
                selectedCategoryId);
            ViewBag.CategoryId = new SelectList(
                categories,
                nameof(CategoryOptionDto.Id),
                nameof(CategoryOptionDto.Name),
                selectedCategoryId);
        }

        private void PopulatePagination(ProductAdminPageResult result)
        {
            ViewBag.PageNumber = result.PageNumber;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.TotalItems = result.TotalItems;
        }

        private void ApplyErrors(ProductCommandResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.Message) && result.Errors.Count == 0)
            {
                ModelState.AddModelError(string.Empty, result.Message);
            }

            foreach (var error in result.Errors)
            {
                foreach (var message in error.Value)
                {
                    ModelState.AddModelError(error.Key, message);
                }
            }
        }

        private static async Task<ProductUpsertRequest> BuildUpsertRequestAsync(
            Product product,
            IFormFile? imageFile)
        {
            return new ProductUpsertRequest
            {
                Id = product.Id == 0 ? null : product.Id,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                ExistingImageFileName = product.ImageURL,
                ImageUpload = await ToImageUploadAsync(imageFile)
            };
        }

        private static async Task<ProductImageUpload?> ToImageUploadAsync(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return null;
            }

            await using var memoryStream = new MemoryStream();
            await imageFile.CopyToAsync(memoryStream);

            return new ProductImageUpload
            {
                OriginalFileName = Path.GetFileName(imageFile.FileName),
                ContentType = imageFile.ContentType ?? string.Empty,
                Content = memoryStream.ToArray()
            };
        }
    }
}
