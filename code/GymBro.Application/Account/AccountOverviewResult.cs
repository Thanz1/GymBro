using GymBro.Core;

namespace GymBro.Application.Account
{
    public sealed class AccountOverviewResult
    {
        public required User User { get; init; }
        public IReadOnlyList<Order> RecentOrders { get; init; } = [];
    }
}
