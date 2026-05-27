using System.Net.Http.Json;
using GymBro.Contracts;
namespace GymBro.Service
{
    public class ReviewService : IReviewService
    {
        private readonly HttpClient _httpClient;
        public ReviewService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // 1. Lấy toàn bộ đánh giá (Dùng cho Admin)
        public async Task<IEnumerable<ReviewDto>> GetAllReviewsAsync()
        {
            // SỬA: Dùng "api/review" cho đồng bộ số ít
            return await _httpClient.GetFromJsonAsync<IEnumerable<ReviewDto>>("api/review")
                   ?? new List<ReviewDto>();
        }

        // 2. Lấy đánh giá theo sản phẩm
        public async Task<IEnumerable<ReviewDto>> GetReviewsByProductIdAsync(int productId)
        {
            // Sửa thành "api/Reviews/product/"
            return await _httpClient.GetFromJsonAsync<IEnumerable<ReviewDto>>($"api/Reviews/product/{productId}")
                   ?? new List<ReviewDto>();
        }

        // 3. Thêm đánh giá mới
        public async Task<bool> AddReviewAsync(ReviewDto reviewDto)
        {
            // LƯU Ý: Phải là "api/Reviews", KHÔNG PHẢI "api/review"
            var response = await _httpClient.PostAsJsonAsync("api/Reviews", reviewDto);
            return response.IsSuccessStatusCode;
        }

        // 4. Xóa đánh giá (Dùng cho Admin)
        public async Task<bool> DeleteReviewAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/review/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
    