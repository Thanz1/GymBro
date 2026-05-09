namespace GymBro.Contracts;

public sealed class ProductDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? ImageURL { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int StockQuantity { get; set; }
}

