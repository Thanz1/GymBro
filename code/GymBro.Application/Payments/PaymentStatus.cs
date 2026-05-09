using System;

namespace GymBro.Application.Payments
{
    public static class PaymentStatus
    {
        public const string AwaitingPayment = "Chá» thanh toÃ¡n";
        public const string PendingVerification = "Chá» xÃ¡c minh";
        public const string CashCollectionPending = "Chá» thu tiá»n";
        public const string Paid = "ÄÃ£ thanh toÃ¡n";
        public const string Cancelled = "ÄÃ£ há»§y";
        public const string Failed = "Tháº¥t báº¡i";

        public static bool Equals(string? left, string? right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        public static bool CanApprove(string? status)
        {
            return Equals(status, AwaitingPayment) || Equals(status, PendingVerification);
        }

        public static bool CanReject(string? status)
        {
            return Equals(status, AwaitingPayment) || Equals(status, PendingVerification);
        }

        public static bool IsPaid(string? status)
        {
            return Equals(status, Paid);
        }

        public static bool IsFinal(string? status)
        {
            return IsPaid(status) || Equals(status, Cancelled) || Equals(status, Failed);
        }

        public static bool IsUnsuccessful(string? status)
        {
            return Equals(status, Cancelled) || Equals(status, Failed);
        }
    }
}
