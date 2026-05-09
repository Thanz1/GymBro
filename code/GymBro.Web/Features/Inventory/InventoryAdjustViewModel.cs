using GymBro.Core;

namespace GymBro.Web.Features.Inventory
{
    public class InventoryAdjustViewModel
    {
        public required Product Product { get; init; }
        public int NewQuantity { get; init; }
        public string? Note { get; init; }
    }
}
