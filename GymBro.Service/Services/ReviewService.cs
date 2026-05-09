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

        // 2. Lấy đánh giá theo từng sản phẩm (Dùng cho trang Details)
        public async Task<IEnumerable<ReviewDto>> GetReviewsByProductIdAsync(int productId)
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<ReviewDto>>($"api/review/product/{productId}")
                   ?? new List<ReviewDto>();
        }

        // 3. Thêm đánh giá mới (Dùng cho khách hàng)
        public async Task<bool> AddReviewAsync(ReviewDto reviewDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/review", reviewDto);
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
    