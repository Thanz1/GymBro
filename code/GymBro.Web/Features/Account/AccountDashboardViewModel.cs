using GymBro.Web.Features.Orders;

namespace GymBro.Web.Features.Account
{
    public class AccountDashboardViewModel
    {
        public required AccountSummaryViewModel User { get; init; }
        public IReadOnlyList<OrderListItemViewModel> RecentOrders { get; init; } = [];
    }
}
