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

        // ĐÃ SỬA: Gom 2 hàm Index làm 1 để triệt tiêu hoàn toàn lỗi AmbiguousMatchException
        // Tích hợp luồng bọc phòng vệ try-catch giúp trang chủ không bị sập khi tắt Product.API
        [HttpGet]
        public async Task<IActionResult> Index(string? keyword = null)
        {
            ViewBag.Keyword = keyword;

            // TRƯỜNG HỢP 1: Nếu người dùng CÓ nhập từ khóa tìm kiếm sản phẩm
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                try
                {
                    // Gọi API tìm kiếm sản phẩm từ Product.API
                    var searchResults = await _productService.SearchProductsAsync(keyword);
                    return View(searchResults ?? new List<ProductDto>());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GYMBRO LỖI] Lỗi kết nối API tìm kiếm: {ex.Message}");
                    return View(new List<ProductDto>()); // API sập trả về danh sách rỗng để bảo vệ trang
                }
            }

            // TRƯỜNG HỢP 2: Giao diện trang chủ mặc định (Khi keyword bị rỗng hoặc null)
            try
            {
                // Lấy dữ liệu tươi từ Product.API
                ViewBag.NewProducts = await _productService.GetNewProductsAsync(8);
                ViewBag.BestSellingProducts = await _productService.GetBestSellingProductsAsync(8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GYMBRO CẢNH BÁO] Product.API đã ngắt kết nối. Kích hoạt chế độ phòng vệ.");
                // API lỗi/sập -> Gán danh sách rỗng để View không bị nổ lỗi NullReferenceException
                ViewBag.NewProducts = new List<ProductDto>();
                ViewBag.BestSellingProducts = new List<ProductDto>();
            }

            return View(new List<ProductDto>());
        }

        // Trang cửa hàng: Tìm kiếm, lọc và sắp xếp
        public async Task<IActionResult> Shop(string keyword, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortOrder, int? page)
        {
            try
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
                ViewBag.Categories = categories?.ToList() ?? new List<CategoryDto>();

                return View(query.ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GYMBRO LỖI] Cửa hàng không thể tải dữ liệu: {ex.Message}");
                ViewBag.Categories = new List<CategoryDto>();
                return View(new List<ProductDto>());
            }
        }

        // Trang chi tiết sản phẩm
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null) return NotFound();

                // Đồng thời gắn ProductId vào ViewBag phục vụ luồng load Ajax Review
                ViewBag.ProductId = id;

                return View(product);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GYMBRO LỖI] Không thể tải chi tiết sản phẩm #{id}: {ex.Message}");
                return NotFound();
            }
        }

        public IActionResult About() => View();
        public IActionResult Contact() => View();
    }
}