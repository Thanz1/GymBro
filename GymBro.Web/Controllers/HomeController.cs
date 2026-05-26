﻿﻿﻿﻿﻿﻿﻿using GymBro.Contracts;
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System;
using System.Threading.Tasks;
namespace GymBro.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        public HomeController(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }

        // Trang chủ: Tích hợp cả tìm kiếm và hiển thị mặc định
        public async Task<IActionResult> Index(string keyword)
        {
            ViewBag.Keyword = keyword;

            // Nếu người dùng CÓ nhập từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                // Gọi API tìm kiếm sản phẩm
                var searchResults = await _productService.SearchProductsAsync(keyword);
                return View(searchResults); // Trả kết quả tìm kiếm ra giao diện
            }

            // Nếu KHÔNG tìm kiếm -> Load giao diện trang chủ mặc định
            ViewBag.NewProducts = await _productService.GetNewProductsAsync(8);
            ViewBag.BestSellingProducts = await _productService.GetBestSellingProductsAsync(8);

            return View();
        }

        // Trang cửa hàng: Tìm kiếm, lọc và sắp xếp
        public async Task<IActionResult> Shop(string keyword, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortOrder, int? page)
        {
            var allProducts = await _productService.GetAllProductsAsync();
            var categories = await _categoryService.GetAllCategoriesAsync();

            var query = allProducts.AsQueryable();

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

            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.ProductName),
                _ => query.OrderByDescending(p => p.Id),
            };

            ViewBag.SortOrder = sortOrder;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Categories = categories.ToList();

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