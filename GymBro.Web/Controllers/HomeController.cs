using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebGymBro.Helpers; // Để dùng Session

namespace GymBro.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly GymBroDbContext _context;

        public HomeController(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Sản phẩm mới nhất
            ViewBag.NewProducts = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .Take(8)
                .ToListAsync();

            // Sản phẩm bán chạy (Demo logic)
            ViewBag.BestSellingProducts = await _context.Products
                .Include(p => p.Category)
                .Take(8)
                .ToListAsync();

            return View();
        }

        // TRANG CỬA HÀNG (SHOP)
        public async Task<IActionResult> Shop(string keyword, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortOrder, string availability, int? page)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            // 1. Lọc theo từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p => p.TenSanPham.Contains(keyword) || p.MoTa.Contains(keyword));
                ViewBag.Keyword = keyword;
            }

            // 2. Lọc danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
                ViewBag.CategoryId = categoryId;
            }

            // 3. Lọc giá
            if (minPrice.HasValue) query = query.Where(p => p.Gia >= minPrice);
            if (maxPrice.HasValue) query = query.Where(p => p.Gia <= maxPrice);

            // 4. Sắp xếp
            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.Gia),
                "price_desc" => query.OrderByDescending(p => p.Gia),
                "name_asc" => query.OrderBy(p => p.TenSanPham),
                _ => query.OrderByDescending(p => p.Id),
            };

            // 5. Lưu ViewBag để giữ trạng thái bộ lọc
            ViewBag.SortOrder = sortOrder;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(await query.ToListAsync());
        }

        // CHI TIẾT SẢN PHẨM
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            // Kiểm tra yêu thích
            var user = HttpContext.Session.GetObject<User>("User");
            if (user != null)
            {
                ViewBag.IsWishlisted = await _context.Wishlists
                    .AnyAsync(w => w.UserId == user.Id && w.ProductId == id);
            }

            return View(product);
        }

        public IActionResult About() => View();
        public IActionResult Contact() => View();
    }
}