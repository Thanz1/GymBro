using GymBro.API.DTOs;
using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GymBro.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly GymBroDbContext _context;
        private readonly IConfiguration _configuration;

        // Tiêm DbContext và Cấu hình vào để dùng
        public AuthController(GymBroDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 1. API ĐĂNG KÝ
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(RegisterDto request)
        {
            // Kiểm tra xem username đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest("Tài khoản đã tồn tại.");
            }

            // Mã hóa mật khẩu (Bảo mật: Không bao giờ lưu mật khẩu gốc)
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Tạo user mới
            var user = new User
            {
                Username = request.Username,
                PasswordHash = passwordHash,
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
            // Tìm user trong DB
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                return BadRequest("Sai tài khoản hoặc mật khẩu.");
            }

            // Kiểm tra mật khẩu (So sánh cái nhập vào với cái đã mã hóa trong DB)
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Sai tài khoản hoặc mật khẩu.");
            }

            // Nếu đúng hết, tạo Token (Vé vào cửa)
            string token = CreateToken(user);
            return Ok(token);
        }

        // Hàm tạo JWT Token (Vé vào cửa)
        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            // Lấy mã bí mật từ appsettings.json
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("Jwt:Key").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1), // Token hết hạn sau 1 ngày
                    signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }
}