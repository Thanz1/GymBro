using System.Net.Http.Json;
using GymBro.Contracts;
namespace GymBro.Service
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        public UserService(HttpClient httpClient) { _httpClient = httpClient; }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync(string? search)
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<UserDto>>($"api/user?search={search}")
                   ?? new List<UserDto>();
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<UserDto>($"api/user/{id}");
        }

        // SỬA: Thêm <int> để nhận Id của User vừa tạo
        public async Task<ServiceResponse<int>> CreateUserAsync(UserDto userDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/user", userDto);
            return await response.Content.ReadFromJsonAsync<ServiceResponse<int>>()
                   ?? new ServiceResponse<int> { Success = response.IsSuccessStatusCode };
        }

        public async Task<bool> UpdateUserAsync(int id, UserDto userDto, string? newPassword)
        {
            var request = new { User = userDto, NewPassword = newPassword };
            var response = await _httpClient.PutAsJsonAsync($"api/user/{id}", request);
            return response.IsSuccessStatusCode;
        }

        // SỬA: Thêm <bool> vì lệnh Delete thường chỉ cần trả về trạng thái đúng/sai
        public async Task<ServiceResponse<bool>> DeleteUserAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/user/{id}");
            return await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>()
                   ?? new ServiceResponse<bool> { Success = response.IsSuccessStatusCode };
        }
    }
}