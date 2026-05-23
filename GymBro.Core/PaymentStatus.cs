namespace GymBro.Core;

public static class PaymentStatus
{
    public const string AwaitingPayment = "Chờ thanh toán";
    public const string PendingVerification = "Chờ xác minh";
    public const string CashCollectionPending = "Chờ thu tiền";
    public const string Paid = "Đã thanh toán";
    public const string Cancelled = "Đã hủy";
    public const string Failed = "Thất bại";

    public static bool Equals(string? left, string? right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    public static bool IsPaid(string? status) => Equals(status, Paid);
}
