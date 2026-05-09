namespace GymBro.Application.Auth
{
    public interface IUserAuthenticationService
    {
        Task<RegisterUserResult> RegisterAsync(RegisterUserRequest request);
        Task<AuthenticationResult> AuthenticateAsync(LoginUserRequest request);
    }
}
