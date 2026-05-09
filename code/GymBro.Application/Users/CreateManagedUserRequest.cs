namespace GymBro.Application.Users
{
    public sealed class CreateManagedUserRequest
    {
        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string? FullName { get; init; }
        public string Email { get; init; } = string.Empty;
        public string? Address { get; init; }
        public string Role { get; init; } = "User";
        public bool IsActive { get; init; } = true;
    }
}
