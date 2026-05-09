using GymBro.Core;
using GymBro.Application.Shared;

namespace GymBro.Application.Catalog
{
    public interface IProductAdminService
    {
        Task<ProductAdminPageResult> GetPageAsync(ProductAdminPageQuery query);
        Task<IReadOnlyList<CategoryOptionDto>> GetCategoryOptionsAsync();
        Task<Product?> GetDetailsAsync(int id);
        Task<Product?> GetEditModelAsync(int id);
        Task<ProductCommandResult> CreateAsync(ProductUpsertRequest request);
        Task<ProductCommandResult> UpdateAsync(ProductUpsertRequest request);
        Task<OperationResult> DeleteAsync(int id);
    }
}
