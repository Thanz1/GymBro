using System.Net.Http.Json;
using GymBro.Contracts;
using GymBro.Contracts.DTOs;
namespace GymBro.Service
{
    public class WishlistService : IWishlistService
    {
        private readonly HttpClient _httpClient;
        public WishlistService(HttpClient httpClient) { _httpClient = httpClient; }

        public async Task<IEnumerable<WishlistDto>> GetWishlistByUserIdAsync(int userId)
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<WishlistDto>>($"api/wishlist/user/{userId}")
                   ?? new List<WishlistDto>();
        }

        public async Task<bool> ToggleWishlistAsync(int userId, int productId)
        {
            var response = await _httpClient.PostAsJsonAsync("api/wishlist/toggle", new { userId, productId });
            if (response.IsSuccessStatusCode)
            {
                // API nên trả về true nếu là Add, false nếu là Remove
                var result = await response.Content.ReadFromJsonAsync<dynamic>();
                return (bool)result?.isAdded;
            }
            return false;
        }
    }
}