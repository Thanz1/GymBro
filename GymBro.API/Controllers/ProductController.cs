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

        // 1. Lấy danh sách tất cả sản phẩm (Ai cũng xem được)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }

        // 2. Thêm sản phẩm mới (Phải Đăng nhập mới được thêm -> Demo Bảo mật)
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Product>> CreateProduct(CreateProductDto request)
        {
            var product = new Product
            {
                TenSanPham = request.TenSanPham,
                Gia = request.Gia,
                MoTa = request.MoTa,
                HinhAnhUrl = request.HinhAnhUrl,
                DanhMuc = request.DanhMuc,
                SoLuongTon = request.SoLuongTon
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(product);
        }
    }
}