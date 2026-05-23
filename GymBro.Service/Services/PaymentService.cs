using System.Net.Http.Json;
using GymBro.Contracts;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GymBro.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly HttpClient _httpClient;

        public PaymentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync()
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<PaymentDto>>("api/payment")
                   ?? new List<PaymentDto>();
        }

        public async Task<PaymentDto?> GetPaymentByIdAsync(int id)
        {
            // Sửa dứt điểm link gọi sang API: không có gạch chéo đầu, không có chữ 's' ở đuôi Payment
            var response = await _httpClient.GetAsync($"api/payment/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PaymentDto>();
            }

            return null;
        }

        public async Task<bool> UpdatePaymentStatusAsync(int paymentId, string newStatus)
        {
            // Gọi sang API nhánh cập nhật trạng thái bằng phương thức PUT
            var response = await _httpClient.PutAsJsonAsync($"api/payment/{paymentId}/status", newStatus);
            return response.IsSuccessStatusCode;
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
    }
}