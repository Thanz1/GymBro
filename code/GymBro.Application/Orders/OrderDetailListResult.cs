using GymBro.Core;

namespace GymBro.Application.Orders
{
    public sealed class OrderDetailListResult
    {
        public int? OrderId { get; init; }
        public IReadOnlyList<OrderDetail> Items { get; init; } = [];
    }
}
