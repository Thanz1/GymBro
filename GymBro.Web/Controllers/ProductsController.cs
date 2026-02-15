using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class ProductsController : BaseAdminController
    {
        private readonly GymBroDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(GymBroDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.Include(p => p.Category).ToListAsync();
            return View(products);
        }

        // GET: Admin/Products/Create
        public IActionResult Create()
        {
<<<<<<< HEAD
=======
            // SỬA: TenDanhMuc -> CategoryName
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "CategoryName");
            return View();
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
<<<<<<< HEAD
                if (imageFile != null && imageFile.Length > 0)
=======
                if (imageFile != null)
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
                {
                    // 1. Tạo đường dẫn thư mục
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Content", "Images");

                    // 2. Tự động tạo thư mục nếu chưa có
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // 3. Tạo tên file và lưu file
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
<<<<<<< HEAD

                    // 4. Lưu đường dẫn ảnh vào object Product
                    product.ImageURL = "/Content/Images/" + uniqueFileName;
=======
                    // SỬA: HinhAnhUrl -> ImageURL
                    product.ImageURL = uniqueFileName;
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
                }

                // --- XÓA DÒNG NÀY ĐI VÌ MODEL CHƯA CÓ ---
                // product.CreatedDate = DateTime.Now; 
                // ----------------------------------------

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
<<<<<<< HEAD

=======
            // SỬA: TenDanhMuc -> CategoryName
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "CategoryName", product.CategoryId);
            return View(product);
        }
    }
}