using GymBro.Core;

namespace GymBro.Application.Users
{
    public sealed class UserAdminCommandResult
    {
        public bool Succeeded { get; private init; }
        public bool NotFound { get; private init; }
        public string? Message { get; private init; }
        public User? User { get; private init; }
        public IReadOnlyDictionary<string, string[]> Errors { get; private init; } =
            new Dictionary<string, string[]>();

        public static UserAdminCommandResult Success(User user, string message)
        {
            return new UserAdminCommandResult
            {
                Succeeded = true,
                User = user,
                Message = message
            };
        }

        public static UserAdminCommandResult Failure(
            IReadOnlyDictionary<string, string[]> errors,
            string? message = null)
        {
            return new UserAdminCommandResult
            {
                Message = message,
                Errors = errors
            };
        }

        public static UserAdminCommandResult NotFoundResult(string message)
        {
            return new UserAdminCommandResult
            {
                NotFound = true,
                Message = message
            };
        }
    }
}
