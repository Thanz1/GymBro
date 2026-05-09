namespace GymBro.Application.Supply
{
    public sealed class InventoryAdjustCommandResult
    {
        public bool Succeeded { get; private init; }
        public bool NotFound { get; private init; }
        public string? Message { get; private init; }
        public InventoryAdjustData? Data { get; private init; }
        public IReadOnlyDictionary<string, string[]> Errors { get; private init; } =
            new Dictionary<string, string[]>();

        public static InventoryAdjustCommandResult Success(string message)
        {
            return new InventoryAdjustCommandResult
            {
                Succeeded = true,
                Message = message
            };
        }

        public static InventoryAdjustCommandResult Failure(
            InventoryAdjustData data,
            IReadOnlyDictionary<string, string[]> errors,
            string? message = null)
        {
            return new InventoryAdjustCommandResult
            {
                Data = data,
                Message = message,
                Errors = errors
            };
        }

        public static InventoryAdjustCommandResult NotFoundResult(string message)
        {
            return new InventoryAdjustCommandResult
            {
                NotFound = true,
                Message = message
            };
        }
    }
}
