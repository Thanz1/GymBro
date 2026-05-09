namespace GymBro.Web.Features.Orders
{
    public sealed class OrderDetailReadOnlyViewModel
    {
        public int Id { get; init; }
        public int OrderId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal Price { get; init; }
        public decimal TotalAmount { get; init; }
        public BadgeViewModel OrderStatus { get; init; } = new();
    }
}
