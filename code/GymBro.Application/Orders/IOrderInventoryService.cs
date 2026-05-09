using GymBro.Core;

namespace GymBro.Application.Orders
{
    public interface IOrderInventoryService
    {
        Task<StockAdjustmentResult> ReserveForOrderAsync(Order order, string note);
        Task<StockAdjustmentResult> ReleaseForOrderAsync(Order order, string note);
    }
}
