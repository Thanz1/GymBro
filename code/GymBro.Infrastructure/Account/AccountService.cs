using System.ComponentModel.DataAnnotations;
using GymBro.Application.Account;
using GymBro.Application.Auth;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Account
{
    public class AccountService : IAccountService
    {
        private readonly GymBroDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public AccountService(
            GymBroDbContext context,
            IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<User?> GetActiveUserAsync(int userId)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Id == userId && user.IsActive);
        }

        public async Task<AccountOverviewResult?> GetOverviewAsync(int userId)
        {
            var user = await GetActiveUserAsync(userId);
            if (user == null)
            {
                return null;
            }

            var recentOrders = await _context.Orders
                .AsNoTracking()
                .Where(order => order.UserId == userId)
                .Include(order => order.Payments)
                .OrderByDescending(order => order.OrderDate)
                .Take(5)
                .ToListAsync();

            return new AccountOverviewResult
            {
                User = user,
                RecentOrders = recentOrders
            };
        }

        public async Task<AccountCommandResult> UpdateProfileAsync(
            int userId,
            UpdateAccountProfileRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == userId && item.IsActive);
            if (user == null)
            {
                return AccountCommandResult.NotFoundResult("Không tìm thấy tài khoản cần cập nhật.");
            }

            var errors = await ValidateProfileAsync(userId, request);
            if (errors.Count > 0)
            {
                return AccountCommandResult.Failure(errors);
            }

            user.Username = request.Username.Trim();
            user.FullName = NormalizeOptionalText(request.FullName) ?? string.Empty;
            user.Email = NormalizeOptionalText(request.Email) ?? string.Empty;
            user.Address = NormalizeOptionalText(request.Address) ?? string.Empty;

            await _context.SaveChangesAsync();

            return AccountCommandResult.Success(user, "Cập nhật thông tin tài khoản thành công.");
        }

        public async Task<AccountCommandResult> ChangePasswordAsync(
            int userId,
            ChangeAccountPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == userId && item.IsActive);
            if (user == null)
            {
                return AccountCommandResult.NotFoundResult("Không tìm thấy tài khoản cần cập nhật.");
            }

            var errors = ValidatePasswordChange(user, request);
            if (errors.Count > 0)
            {
                return AccountCommandResult.Failure(errors);
            }

            user.Password = _passwordHasher.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return AccountCommandResult.Success(user, "Đổi mật khẩu thành công!");
        }

        public async Task<IReadOnlyList<Order>> GetOrderHistoryAsync(int userId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(order => order.UserId == userId)
                .Include(order => order.Payments)
                .OrderByDescending(order => order.OrderDate)
                .ToListAsync();
        }

        public async Task<AccountOrderDetailsResult?> GetOrderDetailsAsync(int userId, int orderId)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(item => item.User)
                .Include(item => item.OrderDetails)
                    .ThenInclude(detail => detail.Product)
                .Include(item => item.Payments)
                .FirstOrDefaultAsync(item => item.Id == orderId && item.UserId == userId);

            if (order == null)
            {
                return null;
            }

            return new AccountOrderDetailsResult
            {
                Order = order,
                LatestPayment = order.Payments
                    .OrderByDescending(payment => payment.Id)
                    .FirstOrDefault()
            };
        }

        private async Task<Dictionary<string, string[]>> ValidateProfileAsync(
            int userId,
            UpdateAccountProfileRequest request)
        {
            var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (request == null)
            {
                AddError(errors, string.Empty, "Dữ liệu cập nhật không hợp lệ.");
                return ToReadOnly(errors);
            }

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                AddError(errors, nameof(request.Username), "Tên đăng nhập không được để trống.");
            }
            else
            {
                var normalizedUsername = request.Username.Trim();

                if (normalizedUsername.Contains(' ') || normalizedUsername.Contains('@'))
                {
                    AddError(errors, nameof(request.Username), "Tên đăng nhập không được chứa khoảng trắng hoặc ký tự @.");
                }
                else if (await _context.Users.AnyAsync(user =>
                    user.Id != userId
                    && user.Username.ToLower() == normalizedUsername.ToLower()))
                {
                    AddError(errors, nameof(request.Username), "Tên đăng nhập đã tồn tại.");
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var normalizedEmail = request.Email.Trim();
                var emailAttribute = new EmailAddressAttribute();

                if (!emailAttribute.IsValid(normalizedEmail))
                {
                    AddError(errors, nameof(request.Email), "Email không đúng định dạng.");
                }
                else if (await _context.Users.AnyAsync(user =>
                    user.Id != userId
                    && user.Email.ToLower() == normalizedEmail.ToLower()))
                {
                    AddError(errors, nameof(request.Email), "Email đã được sử dụng.");
                }
            }

            return ToReadOnly(errors);
        }

        private Dictionary<string, string[]> ValidatePasswordChange(
            User user,
            ChangeAccountPasswordRequest request)
        {
            var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(request.OldPassword))
            {
                AddError(errors, nameof(request.OldPassword), "Vui lòng nhập mật khẩu cũ.");
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                AddError(errors, nameof(request.NewPassword), "Vui lòng nhập mật khẩu mới.");
            }
            else if (request.NewPassword.Length < 6)
            {
                AddError(errors, nameof(request.NewPassword), "Mật khẩu mới phải có ít nhất 6 ký tự.");
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                AddError(errors, nameof(request.ConfirmPassword), "Mật khẩu xác nhận không khớp.");
            }

            if (!string.IsNullOrWhiteSpace(request.OldPassword)
                && !_passwordHasher.VerifyPassword(request.OldPassword, user.Password))
            {
                AddError(errors, nameof(request.OldPassword), "Mật khẩu cũ không chính xác.");
            }

            if (!string.IsNullOrWhiteSpace(request.OldPassword)
                && !string.IsNullOrWhiteSpace(request.NewPassword)
                && request.OldPassword == request.NewPassword)
            {
                AddError(errors, nameof(request.NewPassword), "Mật khẩu mới phải khác mật khẩu cũ.");
            }

            return ToReadOnly(errors);
        }

        private static void AddError(
            IDictionary<string, List<string>> errors,
            string key,
            string message)
        {
            if (!errors.TryGetValue(key, out var values))
            {
                values = [];
                errors[key] = values;
            }

            values.Add(message);
        }

        private static Dictionary<string, string[]> ToReadOnly(
            IDictionary<string, List<string>> errors)
        {
            return errors.ToDictionary(
                item => item.Key,
                item => item.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
