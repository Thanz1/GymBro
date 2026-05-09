using GymBro.Application.Catalog;

namespace GymBro.Application.Layout
{
    public sealed class StorefrontLayoutData
    {
        public IReadOnlyList<CategoryOptionDto> Categories { get; init; } = [];
        public int WishlistCount { get; init; }
    }
}
