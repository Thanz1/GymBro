using GymBro.Core;
using GymBro.Application.Payments;

namespace GymBro.Application.Shopping
{
    public sealed class PaymentPageResult
    {
        public bool NotFound { get; private init; }
        public bool RedirectToHistory { get; private init; }
        public bool IsWarning { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public Order? Order { get; private init; }
        public string PaymentStatus { get; private init; } =
            GymBro.Application.Payments.PaymentStatus.AwaitingPayment;
        public string? QrImage { get; private init; }
        public string? BankId { get; private init; }
        public string? AccountNo { get; private init; }
        public string? AccountName { get; private init; }
        public string? Description { get; private init; }

        public static PaymentPageResult NotFoundResult()
        {
            return new PaymentPageResult
            {
                NotFound = true
            };
        }

        public static PaymentPageResult RedirectResult(string message)
        {
            return new PaymentPageResult
            {
                RedirectToHistory = true,
                Message = message
            };
        }

        public static PaymentPageResult Success(
            Order order,
            string paymentStatus,
            string? qrImage,
            string? bankId,
            string? accountNo,
            string? accountName,
            string? description,
            string? message = null,
            bool isWarning = false)
        {
            return new PaymentPageResult
            {
                Order = order,
                PaymentStatus = string.IsNullOrWhiteSpace(paymentStatus)
                    ? GymBro.Application.Payments.PaymentStatus.AwaitingPayment
                    : paymentStatus,
                QrImage = qrImage,
                BankId = bankId,
                AccountNo = accountNo,
                AccountName = accountName,
                Description = description,
                Message = message ?? string.Empty,
                IsWarning = isWarning
            };
        }
    }
}
