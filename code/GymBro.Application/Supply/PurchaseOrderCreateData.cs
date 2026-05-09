namespace GymBro.Application.Supply
{
    public sealed class PurchaseOrderCreateData
    {
        public int SupplierId { get; init; }
        public DateTime OrderDate { get; init; } = DateTime.Now;
        public IReadOnlyList<LookupOptionDto> SupplierOptions { get; init; } = [];
    }
}
