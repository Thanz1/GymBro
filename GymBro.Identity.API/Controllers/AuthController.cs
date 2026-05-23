using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymBro.Contracts;
using GymBro.Contracts.DTOs;
using GymBro.Core;
using GymBro.Infrastructure;
using GymBro.Infrastructure.Auth;
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

    public AuthController(GymBroDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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
            CreatedDate = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
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
}
