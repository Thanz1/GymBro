using System.ComponentModel.DataAnnotations;
using GymBro.Application.Auth;
using GymBro.Application.Shared;
using GymBro.Application.Users;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Users
{
    public class UserManagementService : IUserManagementService
    {
        private readonly GymBroDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public UserManagementService(
            GymBroDbContext context,
            IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<UserAdminPageResult> GetPageAsync(UserAdminPageQuery query)
        {
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;

            var usersQuery = _context.Users
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.SearchString))
            {
                var normalizedSearch = query.SearchString.Trim();
                usersQuery = usersQuery.Where(user =>
                    user.Username.Contains(normalizedSearch)
                    || user.Email.Contains(normalizedSearch)
                    || user.FullName.Contains(normalizedSearch));
            }

            usersQuery = usersQuery.OrderByDescending(user => user.Id);

            var totalItems = await usersQuery.CountAsync();
            var totalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling((double)totalItems / pageSize);

            if (totalPages > 0 && pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }

            var items = await usersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new UserAdminPageResult
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                SearchString = query.SearchString
            };
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Id == id);
        }

        public async Task<UserAdminCommandResult> CreateAsync(CreateManagedUserRequest request)
        {
            var errors = await ValidateCreateAsync(request);
            if (errors.Count > 0)
            {
                return UserAdminCommandResult.Failure(errors);
            }

            var user = new User
            {
                Username = request.Username.Trim(),
                Password = _passwordHasher.HashPassword(request.Password),
                FullName = NormalizeOptionalText(request.FullName) ?? string.Empty,
                Email = request.Email.Trim(),
                Address = NormalizeOptionalText(request.Address) ?? string.Empty,
                Role = NormalizeRole(request.Role),
                CreatedDate = DateTime.Now,
                IsActive = request.IsActive
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return UserAdminCommandResult.Success(user, "Đã tạo người dùng thành công.");
        }

        public async Task<UserAdminCommandResult> UpdateAsync(UpdateManagedUserRequest request)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(user => user.Id == request.Id);
            if (existingUser == null)
            {
                return UserAdminCommandResult.NotFoundResult("Không tìm thấy người dùng cần cập nhật.");
            }

            var errors = await ValidateUpdateAsync(request, existingUser);
            if (errors.Count > 0)
            {
                return UserAdminCommandResult.Failure(errors);
            }

            existingUser.Username = request.Username.Trim();
            existingUser.FullName = NormalizeOptionalText(request.FullName) ?? string.Empty;
            existingUser.Email = request.Email.Trim();
            existingUser.Address = NormalizeOptionalText(request.Address) ?? string.Empty;
            existingUser.Role = NormalizeRole(request.Role);

            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                existingUser.Password = _passwordHasher.HashPassword(request.NewPassword);
            }

            await _context.SaveChangesAsync();

            return UserAdminCommandResult.Success(existingUser, "Đã cập nhật người dùng thành công.");
        }

        public async Task<OperationResult> DeleteAsync(int id, int? currentUserId)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return OperationResult.NotFoundResult("Không tìm thấy người dùng cần xóa.");
            }

            if (currentUserId.HasValue && currentUserId.Value == id)
            {
                return OperationResult.Failure("Bạn không thể xóa tài khoản của chính mình.");
            }

            if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase)
                && !await _context.Users.AnyAsync(item => item.Id != user.Id && item.Role == "Admin"))
            {
                return OperationResult.Failure("Không thể xóa admin cuối cùng trong hệ thống.");
            }

            if (await _context.Orders.AnyAsync(order => order.UserId == id))
            {
                return OperationResult.Failure("Không thể xóa người dùng này vì đã có đơn hàng. Hãy vô hiệu hóa tài khoản thay vì xóa.");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return OperationResult.Success("Đã xóa người dùng thành công.");
        }

        private async Task<Dictionary<string, string[]>> ValidateCreateAsync(CreateManagedUserRequest request)
        {
            var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            await ValidateIdentityFieldsAsync(
                errors,
                request.Username,
                request.Email,
                request.Role,
                currentUserId: null);

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                AddError(errors, nameof(request.Password), "Mật khẩu không được để trống.");
            }
            else if (request.Password.Length < 6)
            {
                AddError(errors, nameof(request.Password), "Mật khẩu phải có ít nhất 6 ký tự.");
            }

            return ToReadOnly(errors);
        }

        private async Task<Dictionary<string, string[]>> ValidateUpdateAsync(
            UpdateManagedUserRequest request,
            User existingUser)
        {
            var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            await ValidateIdentityFieldsAsync(
                errors,
                request.Username,
                request.Email,
                request.Role,
                existingUser.Id);

            var normalizedRole = NormalizeRole(request.Role);
            var isSelf = request.CurrentUserId.HasValue && request.CurrentUserId.Value == existingUser.Id;
            var isDemotingAdmin = string.Equals(existingUser.Role, "Admin", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase);

            if (isSelf && isDemotingAdmin)
            {
                AddError(errors, nameof(request.Role), "Bạn không thể tự gỡ quyền admin của chính mình.");
            }

            if (isDemotingAdmin
                && !await _context.Users.AnyAsync(user => user.Id != existingUser.Id && user.Role == "Admin"))
            {
                AddError(errors, nameof(request.Role), "Không thể gỡ quyền admin cuối cùng trong hệ thống.");
            }

            if (!string.IsNullOrWhiteSpace(request.NewPassword) && request.NewPassword.Length < 6)
            {
                AddError(errors, nameof(request.NewPassword), "Mật khẩu mới phải có ít nhất 6 ký tự.");
            }

            return ToReadOnly(errors);
        }

        private async Task ValidateIdentityFieldsAsync(
            IDictionary<string, List<string>> errors,
            string username,
            string email,
            string role,
            int? currentUserId)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                AddError(errors, "Username", "Tên đăng nhập không được để trống.");
            }
            else
            {
                var normalizedUsername = username.Trim();
                var usernameExists = await _context.Users.AnyAsync(user =>
                    user.Id != (currentUserId ?? 0)
                    && user.Username.ToLower() == normalizedUsername.ToLower());

                if (usernameExists)
                {
                    AddError(errors, "Username", "Tên đăng nhập đã tồn tại.");
                }
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                AddError(errors, "Email", "Email không được để trống.");
            }
            else
            {
                var normalizedEmail = email.Trim();
                var emailAttribute = new EmailAddressAttribute();

                if (!emailAttribute.IsValid(normalizedEmail))
                {
                    AddError(errors, "Email", "Email không đúng định dạng.");
                }
                else
                {
                    var emailExists = await _context.Users.AnyAsync(user =>
                        user.Id != (currentUserId ?? 0)
                        && user.Email.ToLower() == normalizedEmail.ToLower());

                    if (emailExists)
                    {
                        AddError(errors, "Email", "Email đã được sử dụng.");
                    }
                }
            }

            if (!IsAllowedRole(role))
            {
                AddError(errors, "Role", "Vai trò không hợp lệ.");
            }
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

        private static string NormalizeRole(string? role)
        {
            return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "User";
        }

        private static bool IsAllowedRole(string? role)
        {
            return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "User", StringComparison.OrdinalIgnoreCase);
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
