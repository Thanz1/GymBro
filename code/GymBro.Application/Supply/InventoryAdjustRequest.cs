namespace GymBro.Application.Supply
{
    public sealed class InventoryAdjustRequest
    {
        public int ProductId { get; init; }
        public int NewQuantity { get; init; }
        public string? Note { get; init; }
    }
}
