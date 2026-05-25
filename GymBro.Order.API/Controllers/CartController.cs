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
    // 1. Sửa thành OrderDbContext
    private readonly OrderDbContext _context;
    // 2. Thêm IHttpClientFactory để gọi Product API
    private readonly IHttpClientFactory _httpClientFactory;

    public CartController(OrderDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    // 1. Xem giỏ hàng của tôi
    [HttpGet("my-cart")]
    public async Task<ActionResult<List<CartItemDto>>> GetMyCart()
    {
        // Ép kiểu UserId từ Token (String) sang số nguyên (int)
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId))
            return Unauthorized("Token không hợp lệ hoặc thiếu UserId!");

            if (user == null) return Unauthorized();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .Select(c => new CartItemDto
                {
                    // ĐÃ SỬA: Đồng bộ Tiếng Anh 100%
                    ProductId = c.ProductId,
                    ProductName = c.Product == null ? string.Empty : c.Product.ProductName,
                    Price = c.Product == null ? 0 : c.Product.Price,
                    Quantity = c.Quantity,
                    ImageURL = c.Product == null ? string.Empty : c.Product.ImageURL
                })
                .ToListAsync();

        return Ok(result);
    }

    // 2. Thêm vào giỏ hàng
    [HttpPost("add")]
    public async Task<ActionResult> AddToCart(AddToCartDto request)
    {
        // Lấy và ép kiểu UserId
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId))
            return Unauthorized("Token không hợp lệ hoặc thiếu UserId!");

        var client = _httpClientFactory.CreateClient("ProductService");
        var response = await client.GetAsync($"products/{request.ProductId}");

        if (!response.IsSuccessStatusCode)
            return BadRequest("Sản phẩm không tồn tại trên hệ thống!");

        // Đã fix lỗi CS0019
        var existingItem = await _context.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == request.ProductId);

        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            var cartItem = new CartItem
            {
                UserId = userId, // Đã fix lỗi CS0029 (gán int cho int)
                ProductId = request.ProductId,
                Quantity = request.Quantity
            };
            _context.CartItems.Add(cartItem);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã thêm vào giỏ hàng!" });
    }
}

// Đã fix cảnh báo vàng CS8618 bằng cách thêm dấu '?' cho phép null
public class ProductDto
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Price { get; set; }
    public string? ImageURL { get; set; }
}

    
