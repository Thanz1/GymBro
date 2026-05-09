namespace GymBro.Application.Users
{
    public sealed class UpdateManagedUserRequest
    {
        public int Id { get; init; }
        public string Username { get; init; } = string.Empty;
        public string? FullName { get; init; }
        public string Email { get; init; } = string.Empty;
        public string? Address { get; init; }
        public string Role { get; init; } = "User";
        public string? NewPassword { get; init; }
        public int? CurrentUserId { get; init; }
    }
}
