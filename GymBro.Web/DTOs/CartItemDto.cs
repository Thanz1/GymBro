namespace GymBro.Web.DTOs // Namespace dành riêng cho Web
{
    public class CartItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageURL { get; set; }

        // Thuộc tính này để tính tổng tiền (khớp với code Controller của bạn)
        public decimal ThanhTien => Price * Quantity;
    }
}