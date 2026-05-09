using GymBro.Application.Supply;
using GymBro.Core;

namespace GymBro.Web.Features.PurchaseOrders
{
    public class PurchaseOrderEditViewModel
    {
        public required PurchaseOrder PurchaseOrder { get; init; }
        public IReadOnlyList<LookupOptionDto> ProductOptions { get; init; } = [];
    }
}
