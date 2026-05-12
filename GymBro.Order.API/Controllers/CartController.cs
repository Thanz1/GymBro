using GymBro.Contracts;
using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Order.API.Controllers;

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

            if (user == null) return Unauthorized();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .Select(c => new CartItemDto
                {
                    // ĐÃ SỬA: Đồng bộ Tiếng Anh 100%
                    ProductId = c.ProductId,
                    ProductName = c.Product.ProductName,
                    Price = c.Product.Price,
                    Quantity = c.Quantity,
                    ImageURL = c.Product.ImageURL
                })
                .ToListAsync();

            return Ok(cartItems);
        }

        // 2. Thêm vào giỏ hàng
        [HttpPost("add")]
        public async Task<ActionResult> AddToCart(AddToCartDto request)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null) return Unauthorized();

            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null) return BadRequest("Sản phẩm không tồn tại");

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ProductId == request.ProductId);

            if (existingItem != null)
            {
                // Sửa: Request dùng Quantity
                existingItem.Quantity += request.Quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    UserId = user.Id,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity // Sửa: Request dùng Quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã thêm vào giỏ hàng!" });
        }
    }
