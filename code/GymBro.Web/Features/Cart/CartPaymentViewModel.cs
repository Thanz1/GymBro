using GymBro.Web.Features.Orders;

namespace GymBro.Web.Features.Cart
{
    public class CartPaymentViewModel
    {
        public required OrderReadOnlyViewModel Order { get; init; }
        public string PaymentStatus { get; init; } = string.Empty;
        public string? QrImage { get; init; }
        public string? BankId { get; init; }
        public string? AccountNo { get; init; }
        public string? AccountName { get; init; }
        public string? Description { get; init; }
        public string? NoticeMessage { get; init; }
        public bool IsWarning { get; init; }
    }
}
