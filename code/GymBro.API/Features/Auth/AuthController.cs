using GymBro.API.DTOs;
using GymBro.Application.Auth;
using GymBro.Core;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserAuthenticationService _authenticationService;
        private readonly ITokenService _tokenService;

        public AuthController(
            IUserAuthenticationService authenticationService,
            ITokenService tokenService)
        {
            _authenticationService = authenticationService;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(RegisterDto request)
        {
            var result = await _authenticationService.RegisterAsync(new RegisterUserRequest
            {
                Username = request.Username,
                Password = request.Password,
                FullName = request.FullName,
                Email = request.Email,
                Role = "User"
            });

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors.Values.SelectMany(item => item).FirstOrDefault() ?? "Đăng ký thất bại.");
            }

            return Ok(result.Message);
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(LoginDto request)
        {
            var result = await _authenticationService.AuthenticateAsync(new LoginUserRequest
            {
                Username = request.Username,
                Password = request.Password
            });

            if (!result.Succeeded || result.User == null)
            {
                return BadRequest(result.Message);
            }

            return Ok(_tokenService.CreateToken(result.User));
        }
    }
}
