using System.Net.Http.Json;
using GymBro.Contracts;
namespace GymBro.Service
{
    public class CategoryService : ICategoryService
    {
        private readonly HttpClient _httpClient;

        public CategoryService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<CategoryDto>>("api/category") ?? new List<CategoryDto>();
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<CategoryDto>($"api/category/{id}");
        }

        public async Task<bool> CreateCategoryAsync(CategoryDto categoryDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/category", categoryDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateCategoryAsync(int id, CategoryDto categoryDto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/category/{id}", categoryDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/category/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}