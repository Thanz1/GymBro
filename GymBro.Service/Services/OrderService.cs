using System.Net.Http.Json;
using GymBro.Contracts;
using GymBro.Contracts.DTOs;

namespace GymBro.Service
{
    public class OrderService : IOrderService
    {
        private readonly HttpClient _httpClient;

        public OrderService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // --- CÁC HÀM CŨ CỦA BẠN ---
        public async Task<bool> CreateOrderAsync(AddToCartDto orderDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/order", orderDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PlaceOrderAsync(CheckoutDto checkoutDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/order/place-order", checkoutDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<OrderDto>> GetOrdersByUserIdAsync(int userId)
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<OrderDto>>($"api/order/user/{userId}")
                   ?? new List<OrderDto>();
        }

        public async Task<OrderDto?> GetOrderDetailsAsync(int orderId)
        {
            return await _httpClient.GetFromJsonAsync<OrderDto>($"api/order/{orderId}");
        }

        // --- THÊM 3 HÀM NÀY ĐỂ HẾT LỖI CS0535 ---[cite: 1]

        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<OrderDto>>("api/order")
                   ?? new List<OrderDto>();
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            // Hàm này dùng chung logic với GetOrderDetailsAsync
            return await GetOrderDetailsAsync(id);
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, string status)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/order/{id}/status", status);
            return response.IsSuccessStatusCode;
        }
        public async Task<IEnumerable<OrderDetailDto>> GetOrderDetailsByOrderIdAsync(int orderId)
        {
            // Gọi sang Order.API: api/order/{orderId}/details
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<OrderDetailDto>>($"api/order/{orderId}/details");
            return response ?? new List<OrderDetailDto>();
        }

    }
}