using GymBro.Contracts;
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class CategoriesController : BaseAdminController
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return View(categories);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryDto categoryDto)
        {
            if (ModelState.IsValid)
            {
                var success = await _categoryService.CreateCategoryAsync(categoryDto);
                if (success)
                {
                    TempData["SuccessMessage"] = "Thêm danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // THÊM ĐOẠN NÀY VÀO: Báo lỗi thẳng lên màn hình nếu API từ chối
                    ModelState.AddModelError("", "Lỗi từ API: Không thể thêm danh mục (Có thể do lỗi Token hoặc kết nối).");
                }
            }
            return View(categoryDto);
        }
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryDto categoryDto)
        {
            if (ModelState.IsValid)
            {
                var success = await _categoryService.UpdateCategoryAsync(id, categoryDto);
                if (success)
                {
                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(categoryDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _categoryService.DeleteCategoryAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Xóa danh mục thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}