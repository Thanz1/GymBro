using GymBro.Core;

namespace GymBro.Application.Catalog
{
    public sealed class ProductAdminPageResult
    {
        public IReadOnlyList<Product> Items { get; init; } = [];
        public IReadOnlyList<CategoryOptionDto> Categories { get; init; } = [];
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalItems { get; init; }
        public int TotalPages { get; init; }
        public string? SearchString { get; init; }
        public int? CategoryId { get; init; }
    }
}
