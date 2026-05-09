namespace GymBro.Web.Features.Orders
{
    public sealed class OrderPaymentSummaryViewModel
    {
        public string Method { get; init; } = string.Empty;
        public BadgeViewModel Status { get; init; } = new();
        public DateTime UpdatedAt { get; init; }
        public decimal Amount { get; init; }
        public bool CanOpenPayment { get; init; }
        public string PaymentActionText { get; init; } = string.Empty;
    }
}
