using GymBro.Application.Shared;
using GymBro.Core;

namespace GymBro.Application.Orders
{
    public interface IOrderAdminService
    {
        Task<OrderAdminPageResult> GetPageAsync(OrderAdminPageQuery query);
        Task<Order?> GetDetailsAsync(int id);
        Task<OrderAdminEditData?> GetEditDataAsync(int id);
        Task<Order?> GetDeleteModelAsync(int id);
        Task<OrderAdminCommandResult> UpdateStatusAsync(UpdateOrderStatusRequest request);
        Task<OperationResult> DeleteAsync(int id);
    }
}
