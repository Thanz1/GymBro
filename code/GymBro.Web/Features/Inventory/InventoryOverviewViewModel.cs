using GymBro.Core;

namespace GymBro.Web.Features.Inventory
{
    public class InventoryOverviewViewModel
    {
        public IReadOnlyList<Product> Products { get; init; } = [];
        public string? SearchString { get; init; }
    }
}
