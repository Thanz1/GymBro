﻿using System.Net.Http.Json;
using GymBro.Contracts;
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
            var products = await GetAllProductsAsync();
            return products.OrderByDescending(p => p.Id).Take(count);
        }

        public async Task<IEnumerable<ProductDto>> GetBestSellingProductsAsync(int count)
        {
            var products = await GetAllProductsAsync();
            return products.Take(count);
        }

        public async Task<ProductDto> GetProductByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<ProductDto>($"api/product/{id}");
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
    }
}
