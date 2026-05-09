using GymBro.Web.Features.Orders;

namespace GymBro.Web.Features.Account
{
    public class AccountOrderHistoryViewModel
    {
        public IReadOnlyList<OrderListItemViewModel> Orders { get; init; } = [];
    }
}
