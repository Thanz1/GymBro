using GymBro.Contracts;
using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Product.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly ProductDbContext _context;

        public ReviewsController(ProductDbContext context)
        {
            _context = context;
        }

        // 1. Hàm lấy đánh giá
        [HttpGet("product/{productId}")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // Bắt buộc lấy dữ liệu tươi từ DB
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedDate) // Phải sort ở đây để đảm bảo thứ tự mới nhất
                .ToListAsync();

            return Ok(reviews);
        }

        // 2. Hàm lưu đánh giá mới
        [HttpPost]
        //[Authorize]
        public async Task<IActionResult> AddReview([FromBody] ReviewDto dto)
        {
            // Kiểm tra nhanh xem dto có null không
            if (dto == null || dto.ProductId == 0) return BadRequest("Dữ liệu không hợp lệ");

            var newReview = new Review
            {
                ProductId = dto.ProductId,
                UserId = dto.UserId,
                UserName = dto.UserName,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedDate = DateTime.Now // Tự động lấy giờ hiện tại từ API
            };

            _context.Reviews.Add(newReview);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}