namespace GymBro.API.DTOs
{
    public class CreateProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public string? ImageURL { get; set; }
        public int CategoryId { get; set; } // Sửa DanhMuc -> CategoryId (số)
        public int StockQuantity { get; set; }
    }
}