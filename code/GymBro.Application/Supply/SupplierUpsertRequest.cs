namespace GymBro.Application.Supply
{
    public sealed class SupplierUpsertRequest
    {
        public int? Id { get; init; }
        public string SupplierName { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public string? Email { get; init; }
        public string? Address { get; init; }
        public bool IsActive { get; init; } = true;
    }
}
