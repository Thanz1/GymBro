namespace GymBro.Application.Payments
{
    public sealed class PaymentMethodUpsertRequest
    {
        public int? Id { get; init; }
        public string MethodName { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsActive { get; init; } = true;
    }
}
