﻿﻿﻿using GymBro.Contracts;
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace GymBro.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService; // Thêm để lấy danh mục cho trang Shop

        public HomeController(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }

        // Trang chủ: Hiển thị sản phẩm mới và bán chạy
        public async Task<IActionResult> Index()
        {
            // Lấy 8 sản phẩm mới nhất và 8 sản phẩm bán chạy qua API Port 7002
            ViewBag.NewProducts = await _productService.GetNewProductsAsync(8);
            ViewBag.BestSellingProducts = await _productService.GetBestSellingProductsAsync(8);
            return View();
        }

        // Trang cửa hàng: Tìm kiếm, lọc và sắp xếp
        public async Task<IActionResult> Shop(string keyword, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortOrder, int? page)
        {
            // 1. Lấy dữ liệu từ các API
            var allProducts = await _productService.GetAllProductsAsync();
            var categories = await _categoryService.GetAllCategoriesAsync(); // Lấy danh sách thực tế

            var query = allProducts.AsQueryable();

            // 2. Logic Lọc sản phẩm (Thực hiện tại tầng Web)
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p => p.ProductName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                     || (p.Description != null && p.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
                ViewBag.Keyword = keyword;
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
                ViewBag.CategoryId = categoryId;
            }

            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice);

            // 3. Logic Sắp xếp
            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.ProductName),
                _ => query.OrderByDescending(p => p.Id),
            };

            // 4. Đưa dữ liệu ra View
            ViewBag.SortOrder = sortOrder;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Categories = categories.ToList(); // Đưa danh sách danh mục thật ra sidebar

            return View(query.ToList());
        }

        // Trang chi tiết sản phẩm
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }

        public IActionResult About() => View();
        public IActionResult Contact() => View();
    }
}