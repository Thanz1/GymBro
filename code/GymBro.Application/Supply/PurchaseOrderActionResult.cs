namespace GymBro.Application.Supply
{
    public sealed class PurchaseOrderActionResult
    {
        public bool Succeeded { get; private init; }
        public bool NotFound { get; private init; }
        public string? Message { get; private init; }
        public int? PurchaseOrderId { get; private init; }

        public static PurchaseOrderActionResult Success(int? purchaseOrderId = null, string? message = null)
        {
            return new PurchaseOrderActionResult
            {
                Succeeded = true,
                PurchaseOrderId = purchaseOrderId,
                Message = message
            };
        }

        public static PurchaseOrderActionResult Failure(int? purchaseOrderId, string message)
        {
            return new PurchaseOrderActionResult
            {
                PurchaseOrderId = purchaseOrderId,
                Message = message
            };
        }

        public static PurchaseOrderActionResult NotFoundResult(string message, int? purchaseOrderId = null)
        {
            return new PurchaseOrderActionResult
            {
                NotFound = true,
                PurchaseOrderId = purchaseOrderId,
                Message = message
            };
        }
    }
}
