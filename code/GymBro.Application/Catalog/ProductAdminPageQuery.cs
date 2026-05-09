namespace GymBro.Application.Catalog
{
    public sealed class ProductAdminPageQuery
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SearchString { get; init; }
        public int? CategoryId { get; init; }
    }
}
