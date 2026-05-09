namespace GymBro.Application.Shopping
{
    public sealed class CheckoutPreparationResult
    {
        public IReadOnlyList<CartItemData> Items { get; init; } = [];
        public IReadOnlyList<CheckoutPaymentMethodOption> PaymentMethods { get; init; } = [];
        public bool HasChanges { get; init; }
        public IReadOnlyList<string> Messages { get; init; } = [];
        public decimal TotalPrice { get; init; }
    }
}
