namespace GymBro.Application.Shopping
{
    public sealed class ConfirmOrderPaymentResult
    {
        public bool Succeeded { get; private init; }
        public bool NotFound { get; private init; }
        public bool IsInfo { get; private init; }
        public bool IsWarning { get; private init; }
        public bool RedirectToHistory { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public int? OrderId { get; private init; }
        public bool RedirectToPayment { get; private init; } = true;

        public static ConfirmOrderPaymentResult Success(int orderId, string message)
        {
            return new ConfirmOrderPaymentResult
            {
                Succeeded = true,
                OrderId = orderId,
                Message = message,
                RedirectToPayment = false
            };
        }

        public static ConfirmOrderPaymentResult Info(int orderId, string message)
        {
            return new ConfirmOrderPaymentResult
            {
                IsInfo = true,
                OrderId = orderId,
                Message = message,
                RedirectToPayment = false
            };
        }

        public static ConfirmOrderPaymentResult Warning(int orderId, string message)
        {
            return new ConfirmOrderPaymentResult
            {
                IsWarning = true,
                OrderId = orderId,
                Message = message,
                RedirectToPayment = false
            };
        }

        public static ConfirmOrderPaymentResult Failure(
            int orderId,
            string message,
            bool redirectToPayment = true)
        {
            return new ConfirmOrderPaymentResult
            {
                OrderId = orderId,
                Message = message,
                RedirectToPayment = redirectToPayment
            };
        }

        public static ConfirmOrderPaymentResult RedirectToHistoryResult(string message)
        {
            return new ConfirmOrderPaymentResult
            {
                RedirectToHistory = true,
                Message = message,
                RedirectToPayment = false
            };
        }

        public static ConfirmOrderPaymentResult NotFoundResult(string message)
        {
            return new ConfirmOrderPaymentResult
            {
                NotFound = true,
                Message = message
            };
        }
    }
}
