using GymBro.Core;

namespace GymBro.Application.Supply
{
    public sealed class InventoryHistoryResult
    {
        public IReadOnlyList<InventoryTransaction> Transactions { get; init; } = [];
        public string ProductName { get; init; } = "Tất cả sản phẩm";
        public int? ProductId { get; init; }
    }
}
