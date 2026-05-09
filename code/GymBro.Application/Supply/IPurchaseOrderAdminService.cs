using GymBro.Core;

namespace GymBro.Application.Supply
{
    public interface IPurchaseOrderAdminService
    {
        Task<IReadOnlyList<PurchaseOrder>> GetListAsync();
        Task<PurchaseOrderCreateData> GetCreateDataAsync(PurchaseOrderCreateRequest? draft = null);
        Task<PurchaseOrderCommandResult> CreateAsync(PurchaseOrderCreateRequest request);
        Task<PurchaseOrderEditData?> GetEditDataAsync(int id);
        Task<PurchaseOrder?> GetDeleteDataAsync(int id);
        Task<PurchaseOrderActionResult> AddDetailAsync(PurchaseOrderDetailRequest request);
        Task<PurchaseOrderActionResult> DeleteDetailAsync(int detailId);
        Task<PurchaseOrderActionResult> DeleteAsync(int id);
        Task<PurchaseOrderActionResult> ApproveAsync(int id);
    }
}
