using GymBro.Contracts;
namespace GymBro.Service
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync(string? search);
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<ServiceResponse<int>> CreateUserAsync(UserDto userDto);
        Task<bool> UpdateUserAsync(int id, UserDto userDto, string? newPassword);
        Task<ServiceResponse<bool>> DeleteUserAsync(int id);
    }
}
