using GymBro.Core;

namespace GymBro.Application.Supply
{
    public sealed class InventoryOverviewResult
    {
        public IReadOnlyList<Product> Products { get; init; } = [];
        public string? SearchString { get; init; }
    }
}
