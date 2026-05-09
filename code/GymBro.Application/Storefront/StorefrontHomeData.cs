using GymBro.Core;

namespace GymBro.Application.Storefront
{
    public sealed class StorefrontHomeData
    {
        public IReadOnlyList<Product> NewProducts { get; init; } = [];
        public IReadOnlyList<Product> BestSellingProducts { get; init; } = [];
    }
}
