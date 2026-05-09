using GymBro.Core;

namespace GymBro.Application.Auth
{
    public sealed class RegisterUserResult
    {
        public bool Succeeded { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public User? User { get; private init; }
        public IReadOnlyDictionary<string, string[]> Errors { get; private init; } =
            new Dictionary<string, string[]>();

        public static RegisterUserResult Success(User user, string message)
        {
            return new RegisterUserResult
            {
                Succeeded = true,
                User = user,
                Message = message
            };
        }

        public static RegisterUserResult Failure(
            IReadOnlyDictionary<string, string[]> errors,
            string? message = null)
        {
            return new RegisterUserResult
            {
                Errors = errors,
                Message = message ?? string.Empty
            };
        }
    }
}
