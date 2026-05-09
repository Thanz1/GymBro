namespace GymBro.Web.Features.Orders
{
    public class OrderManagementIndexViewModel
    {
        public IReadOnlyList<OrderListItemViewModel> Items { get; init; } = [];
        public IReadOnlyList<string> StatusOptions { get; init; } = [];
        public string? SearchString { get; init; }
        public string? CurrentStatus { get; init; }
        public int PageNumber { get; init; }
        public int TotalPages { get; init; }
        public int TotalItems { get; init; }
    }
}
