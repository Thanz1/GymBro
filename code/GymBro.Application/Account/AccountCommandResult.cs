using GymBro.Core;

namespace GymBro.Application.Account
{
    public sealed class AccountCommandResult
    {
        public bool Succeeded { get; private init; }
        public bool NotFound { get; private init; }
        public string? Message { get; private init; }
        public User? User { get; private init; }
        public IReadOnlyDictionary<string, string[]> Errors { get; private init; } =
            new Dictionary<string, string[]>();

        public static AccountCommandResult Success(User user, string message)
        {
            return new AccountCommandResult
            {
                Succeeded = true,
                User = user,
                Message = message
            };
        }

        public static AccountCommandResult Failure(
            IReadOnlyDictionary<string, string[]> errors,
            string? message = null)
        {
            return new AccountCommandResult
            {
                Message = message,
                Errors = errors
            };
        }

        public static AccountCommandResult NotFoundResult(string message)
        {
            return new AccountCommandResult
            {
                NotFound = true,
                Message = message
            };
        }
    }
}
