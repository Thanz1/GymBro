using GymBro.Application.Shared;
using GymBro.Core;

namespace GymBro.Application.Catalog
{
    public interface ICategoryAdminService
    {
        Task<IReadOnlyList<Category>> GetListAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<CategoryCommandResult> CreateAsync(CategoryUpsertRequest request);
        Task<CategoryCommandResult> UpdateAsync(CategoryUpsertRequest request);
        Task<OperationResult> DeleteAsync(int id);
    }
}
