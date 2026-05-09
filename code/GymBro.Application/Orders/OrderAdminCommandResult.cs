namespace GymBro.Application.Orders
{
    public sealed class OrderAdminCommandResult
    {
        public bool Succeeded { get; private init; }
        public bool NotFound { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public IReadOnlyDictionary<string, string[]> Errors { get; private init; } =
            new Dictionary<string, string[]>();

        public static OrderAdminCommandResult Success(string message)
        {
            return new OrderAdminCommandResult
            {
                Succeeded = true,
                Message = message
            };
        }

        public static OrderAdminCommandResult Failure(
            IReadOnlyDictionary<string, string[]> errors,
            string? message = null)
        {
            return new OrderAdminCommandResult
            {
                Message = message ?? string.Empty,
                Errors = errors
            };
        }

        public static OrderAdminCommandResult NotFoundResult(string message)
        {
            return new OrderAdminCommandResult
            {
                NotFound = true,
                Message = message
            };
        }
    }
}
