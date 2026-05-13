using GymBro.Contracts;
using GymBro.Contracts.DTOs;
namespace GymBro.Service
{
    public interface IIdentityService
    {
        Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<bool> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<bool> RegisterAsync(RegisterDto registerDto);
        Task<UserDto?> LoginAsync(LoginDto loginDto);
        Task<bool> CheckHealthAsync();
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<bool> HasAdminAsync();
        Task<bool> CreateAdminAsync(RegisterDto adminDto);
    }
}
