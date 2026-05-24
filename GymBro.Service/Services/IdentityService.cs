using System.Net.Http.Json;
using System.Text.Json;
using GymBro.Contracts;
using GymBro.Contracts.DTOs;

namespace GymBro.Service;

public class IdentityService : IIdentityService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
        var response = await _httpClient.PostAsJsonAsync("api/auth/forgot-password", forgotPasswordDto);
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
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UserDto>(json, JsonOptions);
    }

    public async Task<UserDto?> LoginWithGoogleAsync(GoogleLoginDto googleLoginDto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/google", googleLoginDto);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UserDto>(json, JsonOptions);
    }

    public async Task<bool> CheckHealthAsync()
    {
        var response = await _httpClient.GetAsync("api/auth/has-admin");
        return response.IsSuccessStatusCode;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<UserDto>>("api/auth/users", JsonOptions);
        return response ?? [];
    }

    public async Task<bool> HasAdminAsync()
    {
        var response = await _httpClient.GetAsync("api/auth/has-admin");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CreateAdminAsync(RegisterDto adminDto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/create-admin", adminDto);
        return response.IsSuccessStatusCode;
    }
}
