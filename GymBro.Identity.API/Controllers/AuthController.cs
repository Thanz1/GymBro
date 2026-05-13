using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymBro.Contracts;
using GymBro.Contracts.DTOs;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GymBro.API.Controllers
{
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

        // 1. API ĐĂNG KÝ
        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterDto request)
        {
            if (await _context.Users.AnyAsync(u => u.UserName == request.Username))
            {
                return BadRequest("Tài khoản đã tồn tại.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new GymBro.Core.User
            {
                UserName = request.Username,
                PasswordHash = passwordHash, // CHỐT: Dùng PasswordHash của IdentityUser
                FullName = request.FullName,
                Email = request.Email,
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok("Đăng ký thành công!");
        }

        // 2. API ĐĂNG NHẬP
        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(LoginDto request)
        {
            // Tìm người dùng khớp với Username HOẶC Email
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.UserName == request.Username || u.Email == request.Username);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                return BadRequest("Sai tài khoản hoặc mật khẩu.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Sai tài khoản hoặc mật khẩu.");
            }

            string token = CreateToken(user);
            return Ok(token);
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto request)
        {
            // Tìm người dùng theo Tên đăng nhập hoặc Email
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.UserName == request.Identifier || u.Email == request.Identifier);

            if (user != null)
            {
                // Gửi mã token hoặc in ra Console để demo
                var resetToken = Guid.NewGuid().ToString();
                Console.WriteLine($"[GYMBRO]: Khôi phục cho {user.UserName} - Email: {user.Email}");
            }

            return Ok("Yêu cầu đã được ghi nhận.");
        }
        private string CreateToken(GymBro.Core.User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("Jwt:Key").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto request)
        {
            // Tìm người dùng theo Username hoặc Email
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.UserName == request.Identifier || u.Email == request.Identifier);

            if (user == null) return BadRequest("Người dùng không tồn tại.");

            // Mã hóa mật khẩu mới và lưu vào cột PasswordHash
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("Mật khẩu đã được cập nhật thành công.");
        }
    }
}