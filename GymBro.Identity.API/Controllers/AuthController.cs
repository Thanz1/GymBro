using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using GymBro.Contracts;
using GymBro.Contracts.DTOs;
using GymBro.Contracts.Events;
using GymBro.Core;
using GymBro.Identity.API.Messaging;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GymBro.Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly GymBroDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        GymBroDbContext context,
        IConfiguration configuration,
        IIntegrationEventPublisher eventPublisher,
        ILogger<AuthController> logger)
    {
        _context = context;
        _configuration = configuration;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterDto request)
    {
        var username = request.Username?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(username))
            return BadRequest("Tên đăng nhập không được để trống.");

        if (await _context.Users.AnyAsync(u => u.Username == username))
            return BadRequest("Tài khoản đã tồn tại.");

        if (!string.IsNullOrEmpty(email) &&
            await _context.Users.AnyAsync(u => u.Email == email))
            return BadRequest("Email đã được sử dụng.");

        var user = new User
        {
            Username = username,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName?.Trim() ?? string.Empty,
            Email = email,
            Role = "User",
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        await TryPublishUserCreatedEventAsync(user, "Local");
        return Ok("Đăng ký thành công!");
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto request)
    {
        var identifier = request.Username?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(request.Password))
            return BadRequest("Sai tài khoản hoặc mật khẩu.");

        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Username == identifier || u.Email == identifier);

        if (user == null || !user.IsActive)
            return BadRequest("Sai tài khoản hoặc mật khẩu.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            return BadRequest("Sai tài khoản hoặc mật khẩu.");

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
        var identifier = request.Identifier?.Trim() ?? string.Empty;
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Username == identifier || u.Email == identifier);

        if (user != null)
        {
            Console.WriteLine($"[GYMBRO] Yêu cầu khôi phục: {user.Username} ({user.Email})");
        }

        return Ok("Yêu cầu đã được ghi nhận.");
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto request)
    {
        var identifier = request.Identifier?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(request.NewPassword))
            return BadRequest("Thông tin không hợp lệ.");

        if (request.NewPassword.Length < 6)
            return BadRequest("Mật khẩu mới phải có ít nhất 6 ký tự.");

        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Username == identifier || u.Email == identifier);

        if (user == null)
            return BadRequest("Tài khoản không tồn tại.");

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok("Mật khẩu đã được cập nhật.");
    }

    [HttpGet("has-admin")]
    public async Task<IActionResult> HasAdmin()
    {
        var hasAdmin = await _context.Users.AnyAsync(u => u.Role == "Admin");
        return hasAdmin ? Ok() : NotFound();
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
            await TryPublishUserCreatedEventAsync(user, "Google");
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

    private async Task TryPublishUserCreatedEventAsync(User user, string registrationSource)
    {
        try
        {
            await _eventPublisher.PublishUserCreatedAsync(new UserCreatedIntegrationEvent
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                CreatedAt = user.CreatedDate,
                RegistrationSource = registrationSource
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Không gửi được event UserCreated lên RabbitMQ cho UserId={UserId}. Đăng nhập vẫn thành công.",
                user.Id);
        }
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
}
