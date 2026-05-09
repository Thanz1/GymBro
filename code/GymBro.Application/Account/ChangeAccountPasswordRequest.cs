namespace GymBro.Application.Account
{
    public sealed class ChangeAccountPasswordRequest
    {
        public string OldPassword { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
        public string ConfirmPassword { get; init; } = string.Empty;
    }
}
