using GymBro.Core;

namespace GymBro.Application.Orders
{
    public sealed class OrderAdminPageResult
    {
        public IReadOnlyList<Order> Items { get; init; } = [];
        public IReadOnlyList<string> StatusOptions { get; init; } = [];
        public string? SearchString { get; init; }
        public string? CurrentStatus { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalItems { get; init; }
        public int TotalPages { get; init; }
    }
}
