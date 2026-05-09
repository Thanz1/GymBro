using GymBro.Core;

namespace GymBro.Application.Supply
{
    public sealed class PurchaseOrderCommandResult
    {
        public bool Succeeded { get; private init; }
        public bool NotFound { get; private init; }
        public string? Message { get; private init; }
        public PurchaseOrder? PurchaseOrder { get; private init; }
        public IReadOnlyDictionary<string, string[]> Errors { get; private init; } =
            new Dictionary<string, string[]>();

        public static PurchaseOrderCommandResult Success(PurchaseOrder purchaseOrder, string message)
        {
            return new PurchaseOrderCommandResult
            {
                Succeeded = true,
                PurchaseOrder = purchaseOrder,
                Message = message
            };
        }

        public static PurchaseOrderCommandResult Failure(
            IReadOnlyDictionary<string, string[]> errors,
            string? message = null)
        {
            return new PurchaseOrderCommandResult
            {
                Message = message,
                Errors = errors
            };
        }

        public static PurchaseOrderCommandResult NotFoundResult(string message)
        {
            return new PurchaseOrderCommandResult
            {
                NotFound = true,
                Message = message
            };
        }
    }
}
