namespace GymBro.Application.Payments
{
    public static class PaymentAuditAction
    {
        public const string StatusSnapshot = "status_snapshot";
        public const string CustomerConfirmed = "customer_confirmed";
        public const string AdminApproved = "admin_approved";
        public const string AdminRejected = "admin_rejected";
        public const string OrderCancelled = "order_cancelled";
        public const string OrderRestored = "order_restored";
        public const string OrderExpired = "order_expired";
    }
}
