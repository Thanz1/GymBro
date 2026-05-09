using GymBro.Core;

namespace GymBro.Web.Features.Inventory
{
    public class InventoryHistoryViewModel
    {
        public IReadOnlyList<InventoryTransaction> Transactions { get; init; } = [];
        public string ProductName { get; init; } = string.Empty;
        public int? ProductId { get; init; }
    }
}
