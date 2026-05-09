namespace GymBro.Web.Features.Orders
{
    public sealed class OrderDetailIndexViewModel
    {
        public int? OrderId { get; init; }
        public IReadOnlyList<OrderDetailListItemViewModel> Items { get; init; } = [];
    }
}
