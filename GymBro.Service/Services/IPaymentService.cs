using GymBro.Contracts;
namespace GymBro.Service
{
    public interface IPaymentService
    {
        Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync();
        Task<IEnumerable<PaymentMethodDto>> GetAllPaymentMethodsAsync();
        Task<PaymentMethodDto?> GetPaymentMethodByIdAsync(int id);
        Task<bool> CreatePaymentMethodAsync(PaymentMethodDto methodDto);
        Task<bool> UpdatePaymentMethodAsync(int id, PaymentMethodDto methodDto);
        Task<bool> DeletePaymentMethodAsync(int id);
    }
}
