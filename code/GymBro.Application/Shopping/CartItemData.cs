namespace GymBro.Application.Shopping
{
    public sealed class CartItemData
    {
        public int ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int Quantity { get; init; }
        public string? ImageURL { get; init; }
        public decimal ThanhTien => Price * Quantity;
        public decimal TotalAmount => Price * Quantity;
    }
}
