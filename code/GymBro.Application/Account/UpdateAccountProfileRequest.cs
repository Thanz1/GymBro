namespace GymBro.Application.Account
{
    public sealed class UpdateAccountProfileRequest
    {
        public string Username { get; init; } = string.Empty;
        public string? FullName { get; init; }
        public string? Email { get; init; }
        public string? Address { get; init; }
    }
}
