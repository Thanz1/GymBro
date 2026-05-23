using System.Net.Http.Json;
using GymBro.Contracts;
namespace GymBro.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly HttpClient _httpClient;

        public PaymentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<PaymentMethodDto>> GetAllPaymentMethodsAsync()
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<PaymentMethodDto>>("api/paymentmethod")
                   ?? new List<PaymentMethodDto>();
        }

        public async Task<PaymentMethodDto?> GetPaymentMethodByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<PaymentMethodDto>($"api/paymentmethod/{id}");
        }

        public async Task<bool> CreatePaymentMethodAsync(PaymentMethodDto methodDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/paymentmethod", methodDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdatePaymentMethodAsync(int id, PaymentMethodDto methodDto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/paymentmethod/{id}", methodDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeletePaymentMethodAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/paymentmethod/{id}");
            return response.IsSuccessStatusCode;
        }
        public async Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync()
        {
            // Gọi sang Order.API (thường quản lý cả Order và Payment)
            return await _httpClient.GetFromJsonAsync<IEnumerable<PaymentDto>>("api/payment")
                   ?? new List<PaymentDto>();
        }
        public async Task<PaymentDto?> GetPaymentByIdAsync(int id)
        {
            // Đã xóa dấu / ở đầu và chữ s ở cuối chữ Payment
            var response = await _httpClient.GetAsync($"api/payment/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PaymentDto>();
            }

            return null; // Không tìm thấy
        }
    }
}