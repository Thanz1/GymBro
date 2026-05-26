using GymBro.Contracts;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Product.API.Controllers;

[Route("api/product")] // Chốt dùng số ít để đồng bộ với Web Service
[ApiController]
public class ProductController : ControllerBase
{
    private readonly ProductDbContext _context;

    // ĐÃ SỬA: Cập nhật tên trong Constructor
    public ProductController(ProductDbContext context)
    {
        _context = context;
    }

    // 1. Lấy danh sách sản phẩm (Mapping sang ProductDto)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                ProductName = p.ProductName,
                Price = p.Price,
                Description = p.Description,
                ImageURL = p.ImageURL,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.CategoryName : "N/A",
                StockQuantity = p.StockQuantity
            })
            .ToListAsync();

        return Ok(products);
    }

    // 2. Lấy chi tiết sản phẩm theo ID
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var p = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null) return NotFound();

        var dto = new ProductDto
        {
            Id = p.Id,
            ProductName = p.ProductName,
            Price = p.Price,
            Description = p.Description,
            ImageURL = p.ImageURL,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.CategoryName,
            StockQuantity = p.StockQuantity
        };

        return Ok(dto);
    }

    // 3. Thêm sản phẩm mới (Đã cải tiến để trả về ProductDto có ID)
    [HttpPost]
    // [Authorize] // Yêu cầu đăng nhập mới được thêm sản phẩm
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto request)
    {
        var product = new GymBro.Core.Product
        {
            ProductName = request.ProductName,
            Price = request.Price,
            Description = request.Description,
            ImageURL = request.ImageURL,
            CategoryId = request.CategoryId,
            StockQuantity = request.StockQuantity
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Map sang ProductDto để trả về kèm theo ID vừa sinh ra
        var result = new ProductDto
        {
            Id = product.Id,
            ProductName = product.ProductName,
            Price = product.Price,
            Description = product.Description,
            ImageURL = product.ImageURL,
            CategoryId = product.CategoryId,
            StockQuantity = product.StockQuantity
        };

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, result);
    }

    // 4. Cập nhật sản phẩm
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateProduct(int id, ProductDto request)
    {
        if (id != request.Id) return BadRequest("ID không khớp.");

        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.ProductName = request.ProductName;
        product.Price = request.Price;
        product.Description = request.Description;
        product.ImageURL = request.ImageURL;
        product.CategoryId = request.CategoryId;
        product.StockQuantity = request.StockQuantity;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return StatusCode(500, "Lỗi xung đột dữ liệu.");
        }

        return NoContent();
    }

    // 5. Xóa sản phẩm
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    [HttpGet("search")]
    public async Task<IActionResult> SearchProducts([FromQuery] string keyword)
    {
        // Nếu không nhập gì thì trả về toàn bộ danh sách
        if (string.IsNullOrWhiteSpace(keyword))
        {
            var allProducts = await _context.Products.ToListAsync();
            return Ok(allProducts);
        }

        // Tìm kiếm tương đối chứa từ khóa (không phân biệt hoa thường)
        var products = await _context.Products
     .Where(p => p.ProductName.ToLower().Contains(keyword.ToLower()))
     .ToListAsync();

        if (!products.Any())
        {
            return NotFound(new { message = "Không tìm thấy sản phẩm nào khớp với từ khóa." });
        }

        return Ok(products);
    }
}