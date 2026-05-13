﻿using System.Net.Http.Json;
using GymBro.Contracts;
using GymBro.Contracts.DTOs;
namespace GymBro.Service
{
    public class IdentityService : IIdentityService
    {
        private readonly HttpClient _httpClient;

        public IdentityService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/reset-password", resetPasswordDto);
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            // Gửi Identifier (Tên hoặc Email) sang API cổng 7001
            var response = await _httpClient.PostAsJsonAsync("api/auth/forgot-password", forgotPasswordDto);

            // Trả về true nếu API phản hồi thành công (200 OK)
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> RegisterAsync(RegisterDto registerDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", registerDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<UserDto?> LoginAsync(LoginDto loginDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserDto>();
            }
            return null;
        }

        public async Task<bool> CheckHealthAsync()
        {
            var response = await _httpClient.GetAsync("api/auth/check");
            return response.IsSuccessStatusCode;
        }
        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            // Gọi sang Identity.API (Port 7001) để lấy danh sách User
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<UserDto>>("api/auth/users");
            return response ?? new List<UserDto>();
        }
        public async Task<bool> HasAdminAsync()
        {
            // API Port 7001 sẽ kiểm tra bảng Users và trả về 200 nếu có Admin
            var response = await _httpClient.GetAsync("api/auth/has-admin");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CreateAdminAsync(RegisterDto adminDto)
        {
            // Gửi yêu cầu tạo Admin sang Identity.API
            var response = await _httpClient.PostAsJsonAsync("api/auth/create-admin", adminDto);
            return response.IsSuccessStatusCode;
        }
    }
}