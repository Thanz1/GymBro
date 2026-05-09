using GymBro.API.DTOs;
using GymBro.Application.Shopping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymBro.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICustomerCartService _customerCartService;

        public CartController(ICustomerCartService customerCartService)
        {
            _customerCartService = customerCartService;
        }

        [HttpGet("my-cart")]
        public async Task<ActionResult<IReadOnlyList<CartItemData>>> GetMyCart()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var cartItems = await _customerCartService.GetCartAsync(username ?? string.Empty);

            if (cartItems == null)
            {
                return Unauthorized();
            }

            return Ok(cartItems);
        }

        [HttpPost("add")]
        public async Task<ActionResult> AddToCart(AddToCartDto request)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var result = await _customerCartService.AddToCartAsync(
                username ?? string.Empty,
                request.ProductId,
                request.Quantity);

            if (result.UserNotFound)
            {
                return Unauthorized();
            }

            if (!result.Succeeded)
            {
                return BadRequest(result.Message);
            }

            return Ok(new { message = result.Message });
        }
    }
}
