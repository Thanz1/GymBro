using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using GymBro.Contracts;
using GymBro.Contracts.DTOs;
using GymBro.Contracts.Events;
using GymBro.Core;
using GymBro.Identity.API.Email;
using GymBro.Identity.API.Messaging;
using GymBro.Infrastructure;
using GymBro.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
namespace GymBro.Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IdentityDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IWelcomeEmailSender _welcomeEmailSender;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IdentityDbContext context,
        IConfiguration configuration,
        IIntegrationEventPublisher eventPublisher,
        IWelcomeEmailSender welcomeEmailSender,
        ILogger<AuthController> logger)
    {
        _context = context;
        _configuration = configuration;
        _eventPublisher = eventPublisher;
        _welcomeEmailSender = welcomeEmailSender;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterDto request)
    {
        var username = Normalize(request.Username);
        var email = Normalize(request.Email);

        if (string.IsNullOrEmpty(username))
            return BadRequest("Tên đăng nhập không được để trống.");

        if (await FindByIdentifierAsync(username) != null)
            return BadRequest("Tài khoản đã tồn tại.");

        if (!string.IsNullOrEmpty(email) &&
            await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
            return BadRequest("Email đã được sử dụng.");

        var user = new User
        {
            Username = request.Username!.Trim(),
            Password = PasswordHelper.Hash(request.Password),
            FullName = request.FullName?.Trim() ?? string.Empty,
            Email = request.Email?.Trim() ?? string.Empty,
            Role = "User",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        await TryPublishUserCreatedEventAsync(user, "Local");
        return Ok("Đăng ký thành công!");
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto request)
    {
        var identifier = Normalize(request.Username);
        if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(request.Password))
            return BadRequest("Sai tài khoản hoặc mật khẩu.");

        var user = await FindByIdentifierAsync(identifier);
        if (user == null)
            return BadRequest("Sai tài khoản hoặc mật khẩu.");

        if (!user.IsActive)
            return BadRequest("Tài khoản đã bị khóa. Liên hệ quản trị viên.");

        if (!PasswordHelper.Verify(request.Password, user.Password, out var needsRehash))
            return BadRequest("Sai tài khoản hoặc mật khẩu.");

        if (needsRehash)
        {
            user.Password = PasswordHelper.Hash(request.Password);
            await _context.SaveChangesAsync();
        }

        var token = CreateToken(user);

        return Ok(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            Token = token
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto request)
    {
        var identifier = Normalize(request.Identifier);
        var user = await FindByIdentifierAsync(identifier);

        if (user != null)
            Console.WriteLine($"[GYMBRO] Yêu cầu khôi phục: {user.Username} ({user.Email})");

        return Ok("Yêu cầu đã được ghi nhận.");
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto request)
    {
        var identifier = Normalize(request.Identifier);
        if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(request.NewPassword))
            return BadRequest("Thông tin không hợp lệ.");

        if (request.NewPassword.Length < 6)
            return BadRequest("Mật khẩu mới phải có ít nhất 6 ký tự.");

        var user = await FindByIdentifierAsync(identifier);
        if (user == null)
            return BadRequest("Tài khoản không tồn tại. Kiểm tra lại tên đăng nhập hoặc email.");

        user.Password = PasswordHelper.Hash(request.NewPassword);
        user.IsActive = true;
        await _context.SaveChangesAsync();

        return Ok("Mật khẩu đã được cập nhật.");
    }

    [HttpGet("has-admin")]
    public async Task<IActionResult> HasAdmin()
    {
        var hasAdmin = await _context.Users.AnyAsync(u => u.Role == "Admin");
        return hasAdmin ? Ok() : NotFound();
    }

    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateAdmin(RegisterDto request)
    {
        if (await _context.Users.AnyAsync(u => u.Role == "Admin"))
            return BadRequest("Hệ thống đã có tài khoản Admin.");

        var username = request.Username?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(username))
            return BadRequest("Tên đăng nhập không được để trống.");

        if (await FindByIdentifierAsync(Normalize(username)) != null)
            return BadRequest("Tên đăng nhập đã tồn tại.");

        var user = new User
        {
            Username = username,
            Password = PasswordHelper.Hash(request.Password),
            FullName = request.FullName?.Trim() ?? "Quản trị viên",
            Email = request.Email?.Trim() ?? $"{username}@gymbro.local",
            Role = "Admin",
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("google")]
    public async Task<ActionResult<UserDto>> GoogleLogin(GoogleLoginDto request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
            return BadRequest("Token Google không hợp lệ.");

        var clientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId) || clientId.StartsWith("YOUR_", StringComparison.Ordinal))
            return BadRequest("Chưa cấu hình Google ClientId trong appsettings (Google:ClientId).");

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [clientId]
                });
        }
        catch
        {
            return BadRequest("Token Google không hợp lệ hoặc đã hết hạn.");
        }

        var email = payload.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(email))
            return BadRequest("Không lấy được email từ tài khoản Google.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            var username = await GenerateUniqueGoogleUsernameAsync(email, payload.Subject);
            user = new User
            {
                Username = username,
                Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                FullName = payload.Name?.Trim() ?? email,
                Email = email,
                Role = "User",
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var welcomeEmailHandled = await TrySendWelcomeEmailAsync(user, "Google");
            await TryPublishUserCreatedEventAsync(user, "Google", sendWelcomeEmail: !welcomeEmailHandled);
        }
        else if (!user.IsActive)
        {
            return BadRequest("Tài khoản của bạn đã bị khóa.");
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            Token = CreateToken(user)
        });
    }

    private async Task<bool> TrySendWelcomeEmailAsync(User user, string registrationSource)
    {
        try
        {
            await _welcomeEmailSender.SendWelcomeEmailAsync(
                CreateUserCreatedIntegrationEvent(user, registrationSource),
                CancellationToken.None);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Khong gui duoc email chao mung truc tiep cho UserId={UserId}. Dang nhap van thanh cong.",
                user.Id);
            return true;
        }
    }

    private async Task TryPublishUserCreatedEventAsync(
        User user,
        string registrationSource,
        bool sendWelcomeEmail = true)
    {
        try
        {
            var integrationEvent = CreateUserCreatedIntegrationEvent(user, registrationSource);
            integrationEvent.SendWelcomeEmail = sendWelcomeEmail;
            await _eventPublisher.PublishUserCreatedAsync(integrationEvent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Không gửi được event UserCreated lên RabbitMQ cho UserId={UserId}. Đăng nhập vẫn thành công.",
                user.Id);
        }
    }

    private static UserCreatedIntegrationEvent CreateUserCreatedIntegrationEvent(
        User user,
        string registrationSource)
    {
        return new UserCreatedIntegrationEvent
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            CreatedAt = user.CreatedDate,
            RegistrationSource = registrationSource
        };
    }

    private async Task<string> GenerateUniqueGoogleUsernameAsync(string email, string googleSubject)
    {
        var localPart = email.Split('@')[0];
        var baseName = new string(localPart
            .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '.')
            .ToArray());

        if (string.IsNullOrEmpty(baseName))
        {
            baseName = googleSubject.Length >= 8
                ? $"google_{googleSubject[..8]}"
                : "google_user";
        }

        var candidate = baseName;
        var suffix = 0;
        while (await _context.Users.AnyAsync(u => u.Username == candidate))
        {
            suffix++;
            candidate = $"{baseName}{suffix}";
        }

        return candidate;
    }
  private async Task<User?> FindByIdentifierAsync(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return null;

        return await _context.Users.FirstOrDefaultAsync(u =>
            u.Username.ToLower() == identifier ||
            (u.Email != null && u.Email.ToLower() == identifier));
    }

    private static string Normalize(string? value) =>
        value?.Trim().ToLowerInvariant() ?? string.Empty;

    private string CreateToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key chưa được cấu hình trong appsettings.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    [HttpPut("update-profile")]
    public async Task<IActionResult> UpdateProfile(UserDto request)
    {
        var user = await _context.Users.FindAsync(request.Id);
        if (user == null)
            return NotFound("Không tìm thấy tài khoản.");

        // Kiểm tra xem email mới có bị trùng với người khác trong DB không
        var email = Normalize(request.Email);
        if (user.Email?.ToLower() != email && await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
        {
            return BadRequest("Email đã được sử dụng bởi tài khoản khác.");
        }

        // Cập nhật thông tin
        user.FullName = request.FullName?.Trim() ?? string.Empty;
        user.Email = request.Email?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync();
        return Ok();
    }
}
