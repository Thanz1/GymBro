using GymBro.Application.Supply;

namespace GymBro.Web.Features.PurchaseOrders
{
    public class PurchaseOrderCreateViewModel
    {
        public int SupplierId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public IReadOnlyList<LookupOptionDto> SupplierOptions { get; init; } = [];
    }
}
