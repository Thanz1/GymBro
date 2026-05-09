using GymBro.Core;

namespace GymBro.Application.Payments
{
    public sealed class PaymentMethodCommandResult
    {
        public bool Succeeded { get; private init; }
        public bool NotFound { get; private init; }
        public string? Message { get; private init; }
        public PaymentMethod? PaymentMethod { get; private init; }
        public IReadOnlyDictionary<string, string[]> Errors { get; private init; } =
            new Dictionary<string, string[]>();

        public static PaymentMethodCommandResult Success(PaymentMethod paymentMethod, string message)
        {
            return new PaymentMethodCommandResult
            {
                Succeeded = true,
                PaymentMethod = paymentMethod,
                Message = message
            };
        }

        public static PaymentMethodCommandResult Failure(
            IReadOnlyDictionary<string, string[]> errors,
            string? message = null)
        {
            return new PaymentMethodCommandResult
            {
                Message = message,
                Errors = errors
            };
        }

        public static PaymentMethodCommandResult NotFoundResult(string message)
        {
            return new PaymentMethodCommandResult
            {
                NotFound = true,
                Message = message
            };
        }
    }
}
