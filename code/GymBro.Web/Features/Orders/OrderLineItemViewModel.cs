namespace GymBro.Web.Features.Orders
{
    public sealed class OrderLineItemViewModel
    {
        public int ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal Price { get; init; }
        public decimal TotalAmount { get; init; }
    }
}
