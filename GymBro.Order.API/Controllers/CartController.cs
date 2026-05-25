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
    private readonly OrderDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public CartController(OrderDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("my-cart")]
    public async Task<ActionResult<List<CartItemDto>>> GetMyCart()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId))
            return Unauthorized("Token không hợp lệ hoặc thiếu UserId!");

        var cartItems = await _context.CartItems
            .Where(c => c.UserId == userId)
            .ToListAsync();

        var result = new List<CartItemDto>();
        var client = _httpClientFactory.CreateClient("ProductService");

        foreach (var item in cartItems)
        {
            // Gọi sang Product.API lấy thông tin
            var productRes = await client.GetFromJsonAsync<ProductDto>($"products/{item.ProductId}");

            if (productRes != null)
            {
                result.Add(new CartItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = productRes.ProductName,
                    Price = productRes.Price,
                    Quantity = item.Quantity,
                    ImageURL = productRes.ImageURL
                });
            }
        }

        return Ok(result);
    }

    [HttpPost("add")]
    public async Task<ActionResult> AddToCart(AddToCartDto request)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId))
            return Unauthorized("Token không hợp lệ hoặc thiếu UserId!");

        var client = _httpClientFactory.CreateClient("ProductService");
        var response = await client.GetAsync($"products/{request.ProductId}");

        if (!response.IsSuccessStatusCode)
            return BadRequest("Sản phẩm không tồn tại trên hệ thống!");

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
                UserId = userId,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            };
            _context.CartItems.Add(cartItem);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã thêm vào giỏ hàng!" });
    }
}


