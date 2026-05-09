using GymBro.Core;

namespace GymBro.Application.Supply
{
    public sealed class PurchaseOrderEditData
    {
        public required PurchaseOrder PurchaseOrder { get; init; }
        public IReadOnlyList<LookupOptionDto> ProductOptions { get; init; } = [];
    }
}
