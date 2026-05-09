using GymBro.Application.Shared;
using GymBro.Core;

namespace GymBro.Application.Users
{
    public interface IUserManagementService
    {
        Task<UserAdminPageResult> GetPageAsync(UserAdminPageQuery query);
        Task<User?> GetByIdAsync(int id);
        Task<UserAdminCommandResult> CreateAsync(CreateManagedUserRequest request);
        Task<UserAdminCommandResult> UpdateAsync(UpdateManagedUserRequest request);
        Task<OperationResult> DeleteAsync(int id, int? currentUserId);
    }
}
