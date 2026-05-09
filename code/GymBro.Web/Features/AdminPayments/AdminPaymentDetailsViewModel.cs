using GymBro.Application.Payments;
using GymBro.Core;

namespace GymBro.Web.Features.AdminPayments
{
    public class AdminPaymentDetailsViewModel
    {
        public required Payment Payment { get; init; }
        public IReadOnlyList<PaymentAuditLogEntry> AuditLogs { get; init; } = [];
        public double HoursSinceOrder { get; init; }
        public bool IsExpired { get; init; }
    }
}
