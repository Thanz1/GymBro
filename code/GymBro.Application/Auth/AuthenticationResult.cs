using GymBro.Core;

namespace GymBro.Application.Auth
{
    public sealed class AuthenticationResult
    {
        public bool Succeeded { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public User? User { get; private init; }

        public static AuthenticationResult Success(User user)
        {
            return new AuthenticationResult
            {
                Succeeded = true,
                User = user
            };
        }

        public static AuthenticationResult Failure(string message)
        {
            return new AuthenticationResult
            {
                Message = message
            };
        }
    }
}
