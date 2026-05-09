using GymBro.Contracts;

namespace GymBro.Service
{
    public interface ISupplierService
    {
        Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync();
        Task<SupplierDto?> GetSupplierByIdAsync(int id);
        Task<bool> CreateSupplierAsync(SupplierDto supplierDto);
        Task<bool> UpdateSupplierAsync(int id, SupplierDto supplierDto);
        Task<bool> DeleteSupplierAsync(int id);
    }
}
