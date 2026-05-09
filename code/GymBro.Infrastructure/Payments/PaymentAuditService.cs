using GymBro.Application.Payments;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Payments
{
    public class PaymentAuditService : IPaymentAuditService
    {
        private readonly GymBroDbContext _context;

        public PaymentAuditService(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(
            Payment payment,
            string actionType,
            string actorName,
            string message,
            DateTime? createdDate = null)
        {
            if (payment == null || string.IsNullOrWhiteSpace(actionType) || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO dbo.PaymentAuditLogs
(
    PaymentId,
    OrderId,
    ActionType,
    ActorName,
    Message,
    CreatedDate
)
VALUES
(
    {payment.Id},
    {payment.OrderId},
    {TrimToLength(actionType, 50)},
    {ToDbValue(TrimToLength(actorName, 100))},
    {TrimToLength(message, 1000)},
    {createdDate ?? DateTime.Now}
)");
        }

        public async Task EnsureInitialLogAsync(Payment payment)
        {
            if (payment == null)
            {
                return;
            }

            var existingLogs = await LoadLogsAsync(payment.Id);
            if (existingLogs.Count > 0)
            {
                return;
            }

            foreach (var entry in BuildSyntheticTimeline(payment))
            {
                await LogAsync(
                    payment,
                    entry.ActionType,
                    entry.ActorName ?? "Há»‡ thá»‘ng",
                    entry.Message,
                    entry.CreatedDate);
            }
        }

        public async Task<IReadOnlyList<PaymentAuditLogEntry>> GetTimelineAsync(Payment payment)
        {
            var logs = await LoadLogsAsync(payment.Id);

            if (logs.Count > 0)
            {
                return logs;
            }

            return BuildSyntheticTimeline(payment);
        }

        private async Task<List<PaymentAuditLogEntry>> LoadLogsAsync(int paymentId)
        {
            return await _context.Database.SqlQueryRaw<PaymentAuditLogEntry>(
                @"
SELECT
    PaymentAuditLogId,
    PaymentId,
    OrderId,
    ActionType,
    ActorName,
    Message,
    CreatedDate,
    CAST(0 AS bit) AS IsSynthetic
FROM dbo.PaymentAuditLogs
WHERE PaymentId = {0}
ORDER BY CreatedDate DESC, PaymentAuditLogId DESC",
                paymentId)
                .ToListAsync();
        }

        private static IReadOnlyList<PaymentAuditLogEntry> BuildSyntheticTimeline(Payment payment)
        {
            var entries = new List<PaymentAuditLogEntry>();
            var actorName = payment.Order?.User?.FullName ?? payment.Order?.User?.Username ?? "Há»‡ thá»‘ng";

            if (PaymentStatus.Equals(payment.Status, PaymentStatus.PendingVerification))
            {
                entries.Add(new PaymentAuditLogEntry
                {
                    PaymentId = payment.Id,
                    OrderId = payment.OrderId,
                    ActionType = PaymentAuditAction.CustomerConfirmed,
                    ActorName = actorName,
                    Message = "KhÃ¡ch hÃ ng Ä‘Ã£ xÃ¡c nháº­n Ä‘Ã£ chuyá»ƒn khoáº£n vÃ  Ä‘ang chá» admin kiá»ƒm tra.",
                    CreatedDate = payment.PaymentDate,
                    IsSynthetic = true
                });
            }
            else
            {
                entries.Add(new PaymentAuditLogEntry
                {
                    PaymentId = payment.Id,
                    OrderId = payment.OrderId,
                    ActionType = PaymentAuditAction.StatusSnapshot,
                    ActorName = "Há»‡ thá»‘ng",
                    Message = BuildSnapshotMessage(payment),
                    CreatedDate = payment.PaymentDate,
                    IsSynthetic = true
                });
            }

            return entries
                .OrderByDescending(entry => entry.CreatedDate)
                .ToList();
        }

        private static string BuildSnapshotMessage(Payment payment)
        {
            if (PaymentStatus.Equals(payment.Status, PaymentStatus.AwaitingPayment))
            {
                return "ÄÆ¡n hÃ ng Ä‘Ã£ Ä‘Æ°á»£c táº¡o vÃ  Ä‘ang chá» khÃ¡ch thanh toÃ¡n chuyá»ƒn khoáº£n.";
            }

            if (PaymentStatus.Equals(payment.Status, PaymentStatus.CashCollectionPending))
            {
                return "ÄÆ¡n hÃ ng sá»­ dá»¥ng COD vÃ  Ä‘ang chá» thu tiá»n khi giao hÃ ng.";
            }

            if (PaymentStatus.Equals(payment.Status, PaymentStatus.Paid))
            {
                return "Thanh toÃ¡n hiá»‡n Ä‘ang á»Ÿ tráº¡ng thÃ¡i Ä‘Ã£ thanh toÃ¡n.";
            }

            if (PaymentStatus.Equals(payment.Status, PaymentStatus.Failed))
            {
                return "Thanh toÃ¡n hiá»‡n Ä‘ang á»Ÿ tráº¡ng thÃ¡i tháº¥t báº¡i.";
            }

            if (PaymentStatus.Equals(payment.Status, PaymentStatus.Cancelled))
            {
                return "Thanh toÃ¡n hiá»‡n Ä‘ang á»Ÿ tráº¡ng thÃ¡i Ä‘Ã£ há»§y.";
            }

            return $"Tráº¡ng thÃ¡i hiá»‡n táº¡i cá»§a thanh toÃ¡n lÃ  \"{payment.Status}\".";
        }

        private static object ToDbValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
        }

        private static string TrimToLength(string? value, int maxLength)
        {
            var normalizedValue = (value ?? string.Empty).Trim();
            if (normalizedValue.Length <= maxLength)
            {
                return normalizedValue;
            }

            return normalizedValue[..maxLength];
        }
    }
}
