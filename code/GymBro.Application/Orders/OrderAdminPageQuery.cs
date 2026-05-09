namespace GymBro.Application.Orders
{
    public sealed class OrderAdminPageQuery
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SearchString { get; init; }
        public string? Status { get; init; }
    }
}
