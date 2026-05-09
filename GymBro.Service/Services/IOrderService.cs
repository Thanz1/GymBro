using GymBro.Contracts;

namespace GymBro.Service
{
    public interface IOrderService
    {
        Task<bool> CreateOrderAsync(AddToCartDto orderDto);
        Task<bool> PlaceOrderAsync(CheckoutDto checkoutDto);
        Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
        Task<OrderDto?> GetOrderByIdAsync(int id);
        Task<bool> UpdateOrderStatusAsync(int id, string status);
        Task<IEnumerable<OrderDto>> GetOrdersByUserIdAsync(int userId);
        Task<OrderDto?> GetOrderDetailsAsync(int orderId);
        Task<IEnumerable<OrderDetailDto>> GetOrderDetailsByOrderIdAsync(int orderId);
    }
}
