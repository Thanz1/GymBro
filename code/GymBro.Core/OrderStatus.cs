using System;
using System.Collections.Generic;
using System.Linq;

namespace GymBro.Core
{
    public static class OrderStatus
    {
        public const string Pending = "Chờ xử lý";
        public const string PendingPaymentVerification = "Chờ xác minh";
        public const string Processing = "Đang xử lý";
        public const string Confirmed = "Đã xác nhận";
        public const string Shipping = "Đang giao";
        public const string ShippingLegacy = "Đang giao hàng";
        public const string Completed = "Đã giao";
        public const string CompletedLegacy = "Đã giao hàng";
        public const string Cancelled = "Đã hủy";
        public const string CancelledPaymentFailed = "Đã hủy (Thanh toán thất bại)";
        public const string CancelledExpiredPayment = "Đã hủy (Quá hạn thanh toán)";

        public static List<string> GetAllStatuses()
        {
            return new List<string>
            {
                Pending,
                PendingPaymentVerification,
                Processing,
                Confirmed,
                Shipping,
                Completed,
                Cancelled,
                CancelledPaymentFailed,
                CancelledExpiredPayment
            };
        }

        public static List<string> GetEditableStatuses(string? currentStatus = null)
        {
            var statuses = new List<string>
            {
                Pending,
                PendingPaymentVerification,
                Processing,
                Confirmed,
                Shipping,
                Completed,
                Cancelled
            };

            var normalizedCurrentStatus = Normalize(currentStatus);
            if (!string.IsNullOrWhiteSpace(normalizedCurrentStatus) && !Contains(statuses, normalizedCurrentStatus))
            {
                statuses.Add(normalizedCurrentStatus);
            }

            return statuses;
        }

        public static List<string> GetEquivalentStatuses(string? status)
        {
            var normalizedStatus = Normalize(status);
            if (string.IsNullOrWhiteSpace(normalizedStatus))
            {
                return new List<string>();
            }

            return normalizedStatus switch
            {
                Shipping => new List<string> { Shipping, ShippingLegacy },
                Completed => new List<string> { Completed, CompletedLegacy },
                _ => new List<string> { normalizedStatus }
            };
        }

        public static string Normalize(string? status)
        {
            var trimmedStatus = status?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedStatus))
            {
                return string.Empty;
            }

            if (string.Equals(trimmedStatus, ShippingLegacy, StringComparison.OrdinalIgnoreCase))
            {
                return Shipping;
            }

            if (string.Equals(trimmedStatus, CompletedLegacy, StringComparison.OrdinalIgnoreCase))
            {
                return Completed;
            }

            return trimmedStatus;
        }

        public static bool IsKnownStatus(string? status)
        {
            return Contains(GetAllStatuses(), status);
        }

        public static bool IsCancelled(string? status)
        {
            return Equals(status, Cancelled)
                || Equals(status, CancelledPaymentFailed)
                || Equals(status, CancelledExpiredPayment);
        }

        public static bool IsAwaitingPaymentReview(string? status)
        {
            return Equals(status, Pending) || Equals(status, PendingPaymentVerification);
        }

        public static bool IsShipping(string? status)
        {
            return Equals(status, Shipping) || Equals(status, ShippingLegacy);
        }

        public static bool IsCompleted(string? status)
        {
            return Equals(status, Completed) || Equals(status, CompletedLegacy);
        }

        public static bool Equals(string? left, string? right)
        {
            return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
        }

        private static bool Contains(IEnumerable<string> statuses, string? value)
        {
            return statuses.Any(status => Equals(status, value));
        }
    }
}
