namespace GymBro.Application.Orders
{
    public sealed class StockAdjustmentResult
    {
        public bool Succeeded { get; private init; }
        public bool HasChanges { get; private init; }
        public string? Message { get; private init; }

        public static StockAdjustmentResult Success(bool hasChanges)
        {
            return new StockAdjustmentResult
            {
                Succeeded = true,
                HasChanges = hasChanges
            };
        }

        public static StockAdjustmentResult Failure(string message)
        {
            return new StockAdjustmentResult
            {
                Message = message
            };
        }
    }
}
