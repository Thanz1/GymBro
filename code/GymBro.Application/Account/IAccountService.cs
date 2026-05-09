using GymBro.Core;

namespace GymBro.Application.Account
{
    public interface IAccountService
    {
        Task<User?> GetActiveUserAsync(int userId);
        Task<AccountOverviewResult?> GetOverviewAsync(int userId);
        Task<AccountCommandResult> UpdateProfileAsync(int userId, UpdateAccountProfileRequest request);
        Task<AccountCommandResult> ChangePasswordAsync(int userId, ChangeAccountPasswordRequest request);
        Task<IReadOnlyList<Order>> GetOrderHistoryAsync(int userId);
        Task<AccountOrderDetailsResult?> GetOrderDetailsAsync(int userId, int orderId);
    }
}
