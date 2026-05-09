using GymBro.Core;

namespace GymBro.Application.Supply
{
    public sealed class InventoryAdjustData
    {
        public required Product Product { get; init; }
        public int NewQuantity { get; init; }
        public string? Note { get; init; }
    }
}
