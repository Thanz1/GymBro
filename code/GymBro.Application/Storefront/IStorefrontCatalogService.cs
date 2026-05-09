using GymBro.Core;

namespace GymBro.Application.Storefront
{
    public interface IStorefrontCatalogService
    {
        Task<IReadOnlyList<Product>> GetProductsAsync();
        Task<StorefrontHomeData> GetHomeAsync();
        Task<StorefrontShopData> GetShopAsync(StorefrontShopQuery query);
        Task<Product?> GetProductAsync(int id);
        Task<bool> IsWishlistedAsync(int userId, int productId);
        Task<IReadOnlyList<string>> GetSearchSuggestionsAsync(string term);
    }
}
