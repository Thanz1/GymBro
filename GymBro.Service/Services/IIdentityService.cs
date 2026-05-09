using GymBro.Contracts;
namespace GymBro.Service
{
    public interface IIdentityService
    {
        Task<bool> RegisterAsync(RegisterDto registerDto);
        Task<UserDto?> LoginAsync(LoginDto loginDto);
        Task<bool> CheckHealthAsync();
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<bool> HasAdminAsync();
        Task<bool> CreateAdminAsync(RegisterDto adminDto);
    }
}
