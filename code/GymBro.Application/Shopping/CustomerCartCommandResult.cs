namespace GymBro.Application.Shopping
{
    public sealed class CustomerCartCommandResult
    {
        public bool Succeeded { get; private init; }
        public bool UserNotFound { get; private init; }
        public bool ProductNotFound { get; private init; }
        public string Message { get; private init; } = string.Empty;

        public static CustomerCartCommandResult Success(string message)
        {
            return new CustomerCartCommandResult
            {
                Succeeded = true,
                Message = message
            };
        }

        public static CustomerCartCommandResult UserNotFoundResult(string message)
        {
            return new CustomerCartCommandResult
            {
                UserNotFound = true,
                Message = message
            };
        }

        public static CustomerCartCommandResult ProductNotFoundResult(string message)
        {
            return new CustomerCartCommandResult
            {
                ProductNotFound = true,
                Message = message
            };
        }

        public static CustomerCartCommandResult Failure(string message)
        {
            return new CustomerCartCommandResult
            {
                Message = message
            };
        }
    }
}
