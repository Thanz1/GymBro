using GymBro.API.DTOs;
using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly GymBroDbContext _context;

        public ProductController(GymBroDbContext context)
        {
            _context = context;
        }

        // 1. Lấy danh sách sản phẩm
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }

        // 2. Thêm sản phẩm mới
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Product>> CreateProduct(CreateProductDto request)
        {
            var product = new Product
            {
                // SỬA LỖI: Dùng tên biến Tiếng Anh để khớp với Model Product.cs
                ProductName = request.ProductName,
                Price = request.Price,
                Description = request.Description,
                ImageURL = request.ImageURL,
                CategoryId = request.CategoryId,
                StockQuantity = request.StockQuantity
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(product);
        }
    }
}