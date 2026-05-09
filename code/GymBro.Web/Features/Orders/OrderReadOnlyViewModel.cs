namespace GymBro.Web.Features.Orders
{
    public sealed class OrderReadOnlyViewModel
    {
        public int Id { get; init; }
        public DateTime OrderDate { get; init; }
        public decimal TotalAmount { get; init; }
        public BadgeViewModel OrderStatus { get; init; } = new();
        public string CustomerDisplayName { get; init; } = string.Empty;
        public string CustomerUsername { get; init; } = string.Empty;
        public string CustomerEmail { get; init; } = string.Empty;
        public string CustomerAddress { get; init; } = string.Empty;
        public OrderPaymentSummaryViewModel? Payment { get; init; }
        public IReadOnlyList<OrderLineItemViewModel> Items { get; init; } = [];
    }
}
