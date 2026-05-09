using GymBro.Application.Shared;
using GymBro.Core;

namespace GymBro.Application.Payments
{
    public interface IPaymentMethodAdminService
    {
        Task<IReadOnlyList<PaymentMethod>> GetListAsync();
        Task<PaymentMethod?> GetByIdAsync(int id);
        Task<PaymentMethodCommandResult> CreateAsync(PaymentMethodUpsertRequest request);
        Task<PaymentMethodCommandResult> UpdateAsync(PaymentMethodUpsertRequest request);
        Task<OperationResult> DeleteAsync(int id);
    }
}
