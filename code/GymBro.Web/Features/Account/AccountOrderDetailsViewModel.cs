using GymBro.Web.Features.Orders;

namespace GymBro.Web.Features.Account
{
    public class AccountOrderDetailsViewModel
    {
        public required OrderReadOnlyViewModel Order { get; init; }
    }
}
