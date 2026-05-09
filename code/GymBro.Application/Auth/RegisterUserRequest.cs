namespace GymBro.Application.Auth
{
    public sealed class RegisterUserRequest
    {
        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public string Role { get; init; } = "User";
        public bool IsActive { get; init; } = true;
    }
}
