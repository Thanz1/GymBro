using System.Net.Http.Json;
using GymBro.Contracts;
namespace GymBro.Service
{
    public class SupplierService : ISupplierService
    {
        private readonly HttpClient _httpClient;

        public SupplierService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync()
        {
            // Chốt dùng số ít "api/supplier" cho đồng bộ với product nhé
            return await _httpClient.GetFromJsonAsync<IEnumerable<SupplierDto>>("api/supplier")
                   ?? new List<SupplierDto>();
        }

        public async Task<SupplierDto?> GetSupplierByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<SupplierDto>($"api/supplier/{id}");
        }

        public async Task<bool> CreateSupplierAsync(SupplierDto supplierDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/supplier", supplierDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSupplierAsync(int id, SupplierDto supplierDto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/supplier/{id}", supplierDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteSupplierAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/supplier/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}