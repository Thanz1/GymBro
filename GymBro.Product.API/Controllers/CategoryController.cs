using Microsoft.AspNetCore.Mvc;
using GymBro.Contracts;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
namespace GymBro.Product.API.Controllers
{
    [Route("api/category")] // Chốt đúng đường dẫn mà CategoryService đang gọi
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ProductDbContext _context;

        public CategoryController(ProductDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    CategoryName = c.CategoryName
                    // Đã xóa Description ở đây
                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var c = await _context.Categories.FindAsync(id);
            if (c == null) return NotFound();

            var dto = new CategoryDto
            {
                Id = c.Id,
                CategoryName = c.CategoryName
                // Đã xóa Description ở đây
            };

            return Ok(dto);
        }

        [HttpPost]
       // [Authorize]
        public async Task<ActionResult<CategoryDto>> CreateCategory(CategoryDto request)
        {
            var category = new GymBro.Core.Category
            {
                CategoryName = request.CategoryName
                // Đã xóa Description ở đây
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            request.Id = category.Id;
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, request);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCategory(int id, CategoryDto request)
        {
            if (id != request.Id) return BadRequest("ID không khớp.");

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.CategoryName = request.CategoryName;
            // Đã xóa Description ở đây

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
