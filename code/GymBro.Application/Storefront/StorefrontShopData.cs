using GymBro.Core;

namespace GymBro.Application.Storefront
{
    public sealed class StorefrontShopData
    {
        public IReadOnlyList<Product> Products { get; init; } = [];
        public IReadOnlyList<Category> Categories { get; init; } = [];
        public int PageNumber { get; init; }
        public int TotalPages { get; init; }
        public int TotalItems { get; init; }
    }
}
