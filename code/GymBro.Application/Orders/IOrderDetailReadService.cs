using GymBro.Core;

namespace GymBro.Application.Orders
{
    public interface IOrderDetailReadService
    {
        Task<OrderDetailListResult> GetListAsync(int? orderId);
        Task<OrderDetail?> GetByIdAsync(int id);
    }
}
