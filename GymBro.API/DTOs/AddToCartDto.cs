namespace GymBro.API.DTOs
{
    public class AddToCartDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } // Sửa SoLuong -> Quantity
    }

    public class CartItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty; // TenSanPham -> ProductName
        public decimal Price { get; set; } // DonGia -> Price
        public int Quantity { get; set; } // SoLuong -> Quantity
        public string? ImageURL { get; set; } // HinhAnhUrl -> ImageURL
        public decimal TotalAmount => Price * Quantity; // ThanhTien -> TotalAmount
    }
}