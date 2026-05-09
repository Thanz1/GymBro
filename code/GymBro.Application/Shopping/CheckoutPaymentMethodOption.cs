namespace GymBro.Application.Shopping
{
    public sealed class CheckoutPaymentMethodOption
    {
        public int Id { get; init; }
        public string MethodName { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsSupported { get; init; }
        public bool IsBankTransfer { get; init; }
    }
}
