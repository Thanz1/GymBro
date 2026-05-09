using GymBro.Application.Orders;
using GymBro.Application.Payments;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Payments
{
    public class PaymentWorkflowService : IPaymentWorkflowService
    {
        private readonly GymBroDbContext _context;
        private readonly IPaymentAuditService _auditService;
        private readonly IOrderInventoryService _orderInventoryService;

        public PaymentWorkflowService(
            GymBroDbContext context,
            IPaymentAuditService auditService,
            IOrderInventoryService orderInventoryService)
        {
            _context = context;
            _auditService = auditService;
            _orderInventoryService = orderInventoryService;
        }

        public async Task<WorkflowResult> ApprovePaymentAsync(int paymentId, string adminUsername, string? adminNote)
        {
            var payment = await _context.Payments
                .Include(item => item.Order)
                    .ThenInclude(order => order!.User)
                .FirstOrDefaultAsync(item => item.Id == paymentId);

            if (payment == null)
            {
                return WorkflowResult.Failure("KhÃ´ng tÃ¬m tháº¥y thanh toÃ¡n.");
            }

            if (PaymentStatus.IsPaid(payment.Status))
            {
                return WorkflowResult.Warning("Thanh toÃ¡n nÃ y Ä‘Ã£ Ä‘Æ°á»£c duyá»‡t trÆ°á»›c Ä‘Ã³.");
            }

            if (!PaymentStatus.CanApprove(payment.Status))
            {
                return WorkflowResult.Failure("Chá»‰ cÃ³ thá»ƒ duyá»‡t thanh toÃ¡n Ä‘ang á»Ÿ tráº¡ng thÃ¡i chá» thanh toÃ¡n hoáº·c chá» xÃ¡c minh.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _auditService.EnsureInitialLogAsync(payment);

                var actionedAt = DateTime.Now;
                payment.Status = PaymentStatus.Paid;
                payment.PaymentDate = actionedAt;

                if (payment.Order != null && CanMoveOrderToProcessing(payment.Order.Status))
                {
                    payment.Order.Status = OrderStatus.Processing;
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    payment,
                    PaymentAuditAction.AdminApproved,
                    adminUsername,
                    BuildAuditEntry(adminUsername, "duyá»‡t thanh toÃ¡n", actionedAt, adminNote),
                    actionedAt);

                await transaction.CommitAsync();

                return WorkflowResult.Success($"ÄÃ£ duyá»‡t thanh toÃ¡n #{payment.Id} cho Ä‘Æ¡n hÃ ng #{payment.OrderId}.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return WorkflowResult.Failure("Lá»—i khi duyá»‡t thanh toÃ¡n: " + ex.Message);
            }
        }

        public async Task<WorkflowResult> RejectPaymentAsync(int paymentId, string adminUsername, string? rejectReason)
        {
            if (string.IsNullOrWhiteSpace(rejectReason))
            {
                return WorkflowResult.Failure("Vui lÃ²ng nháº­p lÃ½ do tá»« chá»‘i.");
            }

            var payment = await _context.Payments
                .Include(item => item.Order)
                    .ThenInclude(order => order!.User)
                .Include(item => item.Order)
                    .ThenInclude(order => order!.OrderDetails)
                .FirstOrDefaultAsync(item => item.Id == paymentId);

            if (payment == null)
            {
                return WorkflowResult.Failure("KhÃ´ng tÃ¬m tháº¥y thanh toÃ¡n.");
            }

            if (PaymentStatus.IsPaid(payment.Status))
            {
                return WorkflowResult.Failure("KhÃ´ng thá»ƒ tá»« chá»‘i thanh toÃ¡n Ä‘Ã£ Ä‘Æ°á»£c duyá»‡t.");
            }

            if (PaymentStatus.IsUnsuccessful(payment.Status))
            {
                return WorkflowResult.Warning("Thanh toÃ¡n nÃ y Ä‘Ã£ á»Ÿ tráº¡ng thÃ¡i khÃ´ng thÃ nh cÃ´ng.");
            }

            if (!PaymentStatus.CanReject(payment.Status))
            {
                return WorkflowResult.Failure("Tráº¡ng thÃ¡i thanh toÃ¡n hiá»‡n táº¡i khÃ´ng thá»ƒ tá»« chá»‘i.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _auditService.EnsureInitialLogAsync(payment);

                var actionedAt = DateTime.Now;
                payment.Status = PaymentStatus.Failed;
                payment.PaymentDate = actionedAt;

                if (payment.Order != null && !OrderStatus.IsCancelled(payment.Order.Status))
                {
                    var stockResult = await _orderInventoryService.ReleaseForOrderAsync(
                        payment.Order,
                        $"HoÃ n kho do tá»« chá»‘i thanh toÃ¡n #{payment.Id}");

                    if (!stockResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return WorkflowResult.Failure(stockResult.Message ?? "KhÃ´ng thá»ƒ hoÃ n láº¡i tá»“n kho cho Ä‘Æ¡n hÃ ng.");
                    }

                    payment.Order.Status = OrderStatus.CancelledPaymentFailed;
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    payment,
                    PaymentAuditAction.AdminRejected,
                    adminUsername,
                    BuildAuditEntry(adminUsername, "tá»« chá»‘i thanh toÃ¡n", actionedAt, rejectReason),
                    actionedAt);

                await transaction.CommitAsync();

                return WorkflowResult.Success($"ÄÃ£ tá»« chá»‘i thanh toÃ¡n #{payment.Id}.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return WorkflowResult.Failure("Lá»—i khi tá»« chá»‘i thanh toÃ¡n: " + ex.Message);
            }
        }

        private static bool CanMoveOrderToProcessing(string? orderStatus)
        {
            return !OrderStatus.IsCancelled(orderStatus)
                && !OrderStatus.IsCompleted(orderStatus)
                && !OrderStatus.IsShipping(orderStatus);
        }

        private static string BuildAuditEntry(string adminUsername, string action, DateTime actionedAt, string? note)
        {
            var actorName = string.IsNullOrWhiteSpace(adminUsername) ? "Admin" : adminUsername;
            var entry = $"Admin '{actorName}' {action} lÃºc: {actionedAt:dd/MM/yyyy HH:mm:ss}";
            if (!string.IsNullOrWhiteSpace(note))
            {
                entry += " - Ghi chÃº: " + note.Trim();
            }

            return entry;
        }
    }
}
