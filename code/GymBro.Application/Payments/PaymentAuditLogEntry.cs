namespace GymBro.Application.Payments
{
    public class PaymentAuditLogEntry
    {
        public int PaymentAuditLogId { get; set; }
        public int PaymentId { get; set; }
        public int? OrderId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? ActorName { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsSynthetic { get; set; }
    }
}
