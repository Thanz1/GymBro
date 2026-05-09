namespace GymBro.Application.Shared
{
    public sealed class OperationResult
    {
        public bool Succeeded { get; private init; }
        public bool NotFound { get; private init; }
        public string Message { get; private init; } = string.Empty;

        public static OperationResult Success(string message)
        {
            return new OperationResult
            {
                Succeeded = true,
                Message = message
            };
        }

        public static OperationResult Failure(string message)
        {
            return new OperationResult
            {
                Message = message
            };
        }

        public static OperationResult NotFoundResult(string message)
        {
            return new OperationResult
            {
                NotFound = true,
                Message = message
            };
        }
    }
}
