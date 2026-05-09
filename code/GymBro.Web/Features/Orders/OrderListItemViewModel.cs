namespace GymBro.Web.Features.Orders
{
    public sealed class OrderListItemViewModel
    {
        public int Id { get; init; }
        public DateTime OrderDate { get; init; }
        public decimal TotalAmount { get; init; }
        public string CustomerDisplayName { get; init; } = string.Empty;
        public string CustomerUsername { get; init; } = string.Empty;
        public BadgeViewModel OrderStatus { get; init; } = new();
        public OrderPaymentSummaryViewModel? Payment { get; init; }
    }
}
