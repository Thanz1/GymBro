namespace GymBro.Application.Storefront
{
    public sealed class StorefrontShopQuery
    {
        public string? Keyword { get; init; }
        public int? CategoryId { get; init; }
        public decimal? MinPrice { get; init; }
        public decimal? MaxPrice { get; init; }
        public string? SortOrder { get; init; }
        public string? Availability { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 12;
    }
}
