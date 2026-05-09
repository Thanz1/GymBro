namespace GymBro.Application.Supply
{
    public sealed class PurchaseOrderCreateRequest
    {
        public int SupplierId { get; init; }
        public DateTime OrderDate { get; init; } = DateTime.Now;
    }
}
