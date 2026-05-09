using GymBro.Core;

namespace GymBro.Application.Payments
{
    public interface IPaymentAuditService
    {
        Task LogAsync(
            Payment payment,
            string actionType,
            string actorName,
            string message,
            DateTime? createdDate = null);

        Task EnsureInitialLogAsync(Payment payment);

        Task<IReadOnlyList<PaymentAuditLogEntry>> GetTimelineAsync(Payment payment);
    }
}
