using GymBro.Core;

namespace GymBro.Application.Account
{
    public sealed class AccountOrderDetailsResult
    {
        public required Order Order { get; init; }
        public Payment? LatestPayment { get; init; }
    }
}
