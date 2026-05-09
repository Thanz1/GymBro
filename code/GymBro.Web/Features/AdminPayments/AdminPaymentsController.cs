using GymBro.Core;
using GymBro.Application.Payments;
using GymBro.Infrastructure;
using GymBro.Web.Controllers;
using GymBro.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Features.AdminPayments
{
    public class AdminPaymentsController : BaseAdminController
    {
        private readonly GymBroDbContext _context;
        private readonly IPaymentAuditService _auditService;
        private readonly IPaymentWorkflowService _workflowService;

        public AdminPaymentsController(
            GymBroDbContext context,
            IPaymentAuditService auditService,
            IPaymentWorkflowService workflowService)
        {
            _context = context;
            _auditService = auditService;
            _workflowService = workflowService;
        }

        public async Task<IActionResult> Index(string? status = null)
        {
            var query = _context.Payments
                .Include(payment => payment.Order)
                    .ThenInclude(order => order!.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(payment => payment.Status == status);
            }

            var payments = await query
                .OrderByDescending(payment => payment.Status == PaymentStatus.PendingVerification)
                .ThenByDescending(payment => payment.Status == PaymentStatus.AwaitingPayment)
                .ThenByDescending(payment => payment.PaymentDate)
                .ToListAsync();

            var model = new AdminPaymentsIndexViewModel
            {
                Payments = payments,
                CurrentStatus = status,
                PendingCount = await _context.Payments.CountAsync(payment => payment.Status == PaymentStatus.PendingVerification),
                WaitingCount = await _context.Payments.CountAsync(payment => payment.Status == PaymentStatus.AwaitingPayment),
                CompletedCount = await _context.Payments.CountAsync(payment => payment.Status == PaymentStatus.Paid),
                FailedCount = await _context.Payments.CountAsync(payment =>
                    payment.Status == PaymentStatus.Failed || payment.Status == PaymentStatus.Cancelled),
                TotalCount = await _context.Payments.CountAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(item => item.Order)
                    .ThenInclude(order => order!.User)
                .Include(item => item.Order)
                    .ThenInclude(order => order!.OrderDetails)
                        .ThenInclude(detail => detail.Product)
                .FirstOrDefaultAsync(item => item.Id == id.Value);

            if (payment == null)
            {
                return NotFound();
            }

            var orderDate = payment.Order?.OrderDate ?? payment.PaymentDate;

            var model = new AdminPaymentDetailsViewModel
            {
                Payment = payment,
                HoursSinceOrder = (DateTime.Now - orderDate).TotalHours,
                IsExpired = !PaymentStatus.IsFinal(payment.Status) && (DateTime.Now - orderDate).TotalHours > 24,
                AuditLogs = await _auditService.GetTimelineAsync(payment)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? adminNote)
        {
            var result = await _workflowService.ApprovePaymentAsync(id, GetAdminUsername(), adminNote);
            SetWorkflowMessage(result);
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? rejectReason)
        {
            var result = await _workflowService.RejectPaymentAsync(id, GetAdminUsername(), rejectReason);
            SetWorkflowMessage(result);
            return RedirectToAction(nameof(Details), new { id });
        }

        public IActionResult PendingVerification()
        {
            return RedirectToAction(nameof(Index), new { status = PaymentStatus.PendingVerification });
        }

        private string GetAdminUsername()
        {
            var admin = HttpContext.Session.GetObject<User>("User");
            return string.IsNullOrWhiteSpace(admin?.Username) ? "Admin" : admin.Username;
        }

        private void SetWorkflowMessage(WorkflowResult result)
        {
            if (string.IsNullOrWhiteSpace(result.Message))
            {
                return;
            }

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = result.Message;
                return;
            }

            TempData[result.IsWarning ? "WarningMessage" : "ErrorMessage"] = result.Message;
        }
    }
}
