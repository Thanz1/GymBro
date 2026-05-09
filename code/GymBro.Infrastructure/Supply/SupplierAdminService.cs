using System.ComponentModel.DataAnnotations;
using GymBro.Application.Shared;
using GymBro.Application.Supply;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Supply
{
    public class SupplierAdminService : ISupplierAdminService
    {
        private readonly GymBroDbContext _context;

        public SupplierAdminService(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Supplier>> GetListAsync()
        {
            return await _context.Suppliers
                .AsNoTracking()
                .OrderByDescending(supplier => supplier.IsActive)
                .ThenBy(supplier => supplier.SupplierName)
                .ToListAsync();
        }

        public async Task<Supplier?> GetByIdAsync(int id)
        {
            return await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(supplier => supplier.Id == id);
        }

        public async Task<SupplierCommandResult> CreateAsync(SupplierUpsertRequest request)
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return SupplierCommandResult.Failure(errors);
            }

            var supplier = new Supplier
            {
                SupplierName = request.SupplierName.Trim(),
                Phone = NormalizeOptionalText(request.Phone),
                Email = NormalizeOptionalText(request.Email),
                Address = NormalizeOptionalText(request.Address),
                IsActive = request.IsActive
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            return SupplierCommandResult.Success(supplier, "Đã tạo nhà cung cấp thành công.");
        }

        public async Task<SupplierCommandResult> UpdateAsync(SupplierUpsertRequest request)
        {
            if (!request.Id.HasValue)
            {
                return SupplierCommandResult.NotFoundResult("Không tìm thấy nhà cung cấp cần cập nhật.");
            }

            var supplier = await _context.Suppliers.FindAsync(request.Id.Value);
            if (supplier == null)
            {
                return SupplierCommandResult.NotFoundResult("Không tìm thấy nhà cung cấp cần cập nhật.");
            }

            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return SupplierCommandResult.Failure(errors);
            }

            supplier.SupplierName = request.SupplierName.Trim();
            supplier.Phone = NormalizeOptionalText(request.Phone);
            supplier.Email = NormalizeOptionalText(request.Email);
            supplier.Address = NormalizeOptionalText(request.Address);
            supplier.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return SupplierCommandResult.Success(supplier, "Đã cập nhật nhà cung cấp thành công.");
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return OperationResult.NotFoundResult("Không tìm thấy nhà cung cấp cần xóa.");
            }

            var hasPurchaseOrders = await _context.PurchaseOrders.AnyAsync(order => order.SupplierId == id);
            if (hasPurchaseOrders)
            {
                return OperationResult.Failure("Không thể xóa nhà cung cấp này vì đã có phiếu nhập liên quan.");
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            return OperationResult.Success("Đã xóa nhà cung cấp thành công.");
        }

        private static Dictionary<string, string[]> Validate(SupplierUpsertRequest request)
        {
            var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(request.SupplierName))
            {
                AddError(errors, nameof(request.SupplierName), "Tên nhà cung cấp không được để trống.");
            }

            var email = NormalizeOptionalText(request.Email);
            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailAttribute = new EmailAddressAttribute();
                if (!emailAttribute.IsValid(email))
                {
                    AddError(errors, nameof(request.Email), "Email không đúng định dạng.");
                }
            }

            return errors.ToDictionary(
                item => item.Key,
                item => item.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }

        private static void AddError(
            IDictionary<string, List<string>> errors,
            string key,
            string message)
        {
            if (!errors.TryGetValue(key, out var values))
            {
                values = [];
                errors[key] = values;
            }

            values.Add(message);
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
