﻿using System.Net.Http.Json;
using GymBro.Contracts;
using Newtonsoft.Json;
namespace GymBro.Service
{
    public class ProductService : IProductService
    {
        private readonly HttpClient _httpClient;

        public ProductService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<ProductDto>>("api/product") ?? new List<ProductDto>();
        }

        public async Task<IEnumerable<ProductDto>> GetNewProductsAsync(int count)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<IEnumerable<ProductDto>>($"api/product?count={count}");
                return response ?? new List<ProductDto>();
            }
            catch (HttpRequestException) // Bắt lỗi khi API sập hoặc mất kết nối
            {
                // Ghi log lỗi ra console để lập trình viên biết
                Console.WriteLine("[GYMBRO CẢNH BÁO] Product.API đã sập hoặc không phản hồi. Trả về danh sách rỗng.");

                // Trả về danh sách trống thay vì nổ lỗi sập trang web
                return new List<ProductDto>();
            }
        }

        public async Task<IEnumerable<ProductDto>> GetBestSellingProductsAsync(int count)
        {
            var products = await GetAllProductsAsync();
            return products.Take(count);
        }

        public async Task<ProductDto> GetProductByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<ProductDto>($"api/product/{id}")
                ?? throw new InvalidOperationException($"Product {id} was not found.");
        }

        public async Task<bool> CreateProductAsync(CreateProductDto productDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/product", productDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateProductAsync(int id, ProductDto productDto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/product/{id}", productDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/product/{id}");
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> AdjustStockAsync(int id, int newQuantity, string note)
        {
            // Tạo một object nặc danh để gửi đi
            var request = new { NewQuantity = newQuantity, Note = note };
            var response = await _httpClient.PostAsJsonAsync($"api/product/{id}/adjust-stock", request);
            return response.IsSuccessStatusCode;
        }
        public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string keyword)
        {
            // Bắn request lên Gateway, Gateway sẽ tự chuyển xuống Product.API
            string url = string.IsNullOrWhiteSpace(keyword)
                ? "/product-api/products"
                : $"/product-api/products/search?keyword={keyword}";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IEnumerable<ProductDto>>(content);
            }

            return new List<ProductDto>(); // Trả về list rỗng nếu lỗi
        }
    }
}
