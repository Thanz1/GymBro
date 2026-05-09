using GymBro.Application.Shopping;

namespace GymBro.Web.Features.Cart
{
    public class CartCheckoutViewModel
    {
        public required CheckoutCustomerViewModel User { get; init; }
        public IReadOnlyList<CartItemData> Items { get; init; } = [];
        public IReadOnlyList<CheckoutPaymentMethodOption> PaymentMethods { get; init; } = [];
        public decimal TotalPrice { get; init; }
    }
}
