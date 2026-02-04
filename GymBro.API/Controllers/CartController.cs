using GymBro.API.DTOs;
using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GymBro.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bắt buộc phải đăng nhập mới dùng được Giỏ hàng
    public class CartController : ControllerBase
    {
        private readonly GymBroDbContext _context;

        public CartController(GymBroDbContext context)
        {
            _context = context;
        }

        // 1. Xem giỏ hàng của tôi
        [HttpGet("my-cart")]
        public async Task<ActionResult<List<CartItemDto>>> GetMyCart()
        {
            // Lấy Username từ Token (người đang đăng nhập)
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return Unauthorized();

            // Lấy danh sách items trong giỏ
            var cartItems = await _context.CartItems
                .Include(c => c.Product) // Kèm thông tin sản phẩm
                .Where(c => c.UserId == user.Id)
                .Select(c => new CartItemDto
                {
                    TenSanPham = c.Product.TenSanPham,
                    DonGia = c.Product.Gia,
                    SoLuong = c.SoLuong
                })
                .ToListAsync();

            return Ok(cartItems);
        }

        // 2. Thêm vào giỏ hàng
        [HttpPost("add")]
        public async Task<ActionResult> AddToCart(AddToCartDto request)
        {
            // Lấy User đang đăng nhập
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            // Kiểm tra sản phẩm có tồn tại không
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null) return BadRequest("Sản phẩm không tồn tại");

            // Kiểm tra xem đã có trong giỏ chưa
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ProductId == request.ProductId);

            if (existingItem != null)
            {
                // Nếu có rồi thì cộng dồn số lượng
                existingItem.SoLuong += request.SoLuong;
            }
            else
            {
                // Chưa có thì tạo mới
                var cartItem = new CartItem
                {
                    UserId = user.Id,
                    ProductId = request.ProductId,
                    SoLuong = request.SoLuong
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return Ok("Đã thêm vào giỏ hàng!");
        }
    }
}