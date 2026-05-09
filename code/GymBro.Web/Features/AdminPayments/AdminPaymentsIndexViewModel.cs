using GymBro.Core;

namespace GymBro.Web.Features.AdminPayments
{
    public class AdminPaymentsIndexViewModel
    {
        public IReadOnlyList<Payment> Payments { get; init; } = [];
        public string? CurrentStatus { get; init; }
        public int PendingCount { get; init; }
        public int WaitingCount { get; init; }
        public int CompletedCount { get; init; }
        public int FailedCount { get; init; }
        public int TotalCount { get; init; }
    }
}
