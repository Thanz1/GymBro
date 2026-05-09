namespace GymBro.Application.Supply
{
    public sealed class PurchaseOrderDetailRequest
    {
        public int PurchaseOrderId { get; init; }
        public int ProductId { get; init; }
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
    }
}
