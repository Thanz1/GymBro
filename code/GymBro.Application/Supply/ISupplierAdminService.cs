using GymBro.Application.Shared;
using GymBro.Core;

namespace GymBro.Application.Supply
{
    public interface ISupplierAdminService
    {
        Task<IReadOnlyList<Supplier>> GetListAsync();
        Task<Supplier?> GetByIdAsync(int id);
        Task<SupplierCommandResult> CreateAsync(SupplierUpsertRequest request);
        Task<SupplierCommandResult> UpdateAsync(SupplierUpsertRequest request);
        Task<OperationResult> DeleteAsync(int id);
    }
}
