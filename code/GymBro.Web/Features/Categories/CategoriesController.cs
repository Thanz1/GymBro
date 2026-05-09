using GymBro.Application.Catalog;
using GymBro.Core;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class CategoriesController : BaseAdminController
    {
        private readonly ICategoryAdminService _categoryAdminService;

        public CategoriesController(ICategoryAdminService categoryAdminService)
        {
            _categoryAdminService = categoryAdminService;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _categoryAdminService.GetListAsync();
            return View(categories);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue)
            {
                return BadRequest();
            }

            var category = await _categoryAdminService.GetByIdAsync(id.Value);
            return category == null ? NotFound() : View(category);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            var result = await _categoryAdminService.CreateAsync(new CategoryUpsertRequest
            {
                CategoryName = category.CategoryName
            });

            if (!result.Succeeded)
            {
                AddModelErrors(result.Errors);
                return View(category);
            }

            TempData["SuccessMessage"] = result.Message ?? "Đã tạo danh mục thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue)
            {
                return BadRequest();
            }

            var category = await _categoryAdminService.GetByIdAsync(id.Value);
            return category == null ? NotFound() : View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id)
            {
                return BadRequest();
            }

            var result = await _categoryAdminService.UpdateAsync(new CategoryUpsertRequest
            {
                Id = id,
                CategoryName = category.CategoryName
            });

            if (result.NotFound)
            {
                TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy danh mục cần cập nhật.";
                return RedirectToAction(nameof(Index));
            }

            if (!result.Succeeded)
            {
                AddModelErrors(result.Errors);
                return View(category);
            }

            TempData["SuccessMessage"] = result.Message ?? "Đã cập nhật danh mục thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!id.HasValue)
            {
                return BadRequest();
            }

            var category = await _categoryAdminService.GetByIdAsync(id.Value);
            return category == null ? NotFound() : View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _categoryAdminService.DeleteAsync(id);

            if (result.NotFound)
            {
                TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy danh mục cần xóa.";
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
