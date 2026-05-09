using System.ComponentModel.DataAnnotations;
using GymBro.Application.Auth;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Auth
{
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly GymBroDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public UserAuthenticationService(
            GymBroDbContext context,
            IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<RegisterUserResult> RegisterAsync(RegisterUserRequest request)
        {
            var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var normalizedUsername = request.Username?.Trim() ?? string.Empty;
            var normalizedPassword = request.Password ?? string.Empty;
            var normalizedEmail = request.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedUsername))
            {
                AddError(errors, nameof(request.Username), "Tài khoản không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(normalizedPassword))
            {
                AddError(errors, nameof(request.Password), "Mật khẩu không được để trống.");
            }
            else if (normalizedPassword.Length < 6)
            {
                AddError(errors, nameof(request.Password), "Mật khẩu phải có ít nhất 6 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                AddError(errors, nameof(request.Email), "Email không được để trống.");
            }
            else if (!new EmailAddressAttribute().IsValid(normalizedEmail))
            {
                AddError(errors, nameof(request.Email), "Email không đúng định dạng.");
            }

            if (!string.IsNullOrWhiteSpace(normalizedUsername)
                && await _context.Users.AnyAsync(user =>
                    (user.Username ?? string.Empty).ToLower() == normalizedUsername.ToLower()))
            {
                AddError(errors, nameof(request.Username), "Tài khoản đã tồn tại.");
            }

            if (!string.IsNullOrWhiteSpace(normalizedEmail)
                && await _context.Users.AnyAsync(user =>
                    (user.Email ?? string.Empty).ToLower() == normalizedEmail.ToLower()))
            {
                AddError(errors, nameof(request.Email), "Email đã được sử dụng.");
            }

            if (errors.Count > 0)
            {
                return RegisterUserResult.Failure(ToReadOnly(errors));
            }

            var user = new User
            {
                Username = normalizedUsername,
                Password = _passwordHasher.HashPassword(normalizedPassword),
                FullName = request.FullName?.Trim() ?? string.Empty,
                Email = normalizedEmail,
                Address = request.Address?.Trim() ?? string.Empty,
                Role = NormalizeRole(request.Role),
                CreatedDate = DateTime.Now,
                IsActive = request.IsActive
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RegisterUserResult.Success(user, "Đăng ký thành công!");
        }

        public async Task<AuthenticationResult> AuthenticateAsync(LoginUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return AuthenticationResult.Failure("Sai tài khoản hoặc mật khẩu.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.Password))
            {
                return AuthenticationResult.Failure("Sai tài khoản hoặc mật khẩu.");
            }

            return AuthenticationResult.Success(user);
        }

        private static string NormalizeRole(string? role)
        {
            return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
                ? "Admin"
                : "User";
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

        private static IReadOnlyDictionary<string, string[]> ToReadOnly(
            IDictionary<string, List<string>> errors)
        {
            return errors.ToDictionary(
                item => item.Key,
                item => item.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
