using GymBro.Contracts;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace GymBro.Service
{
    public interface IPaymentService
    {
        Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync();
        Task<PaymentDto?> GetPaymentByIdAsync(int id);
        Task<bool> UpdatePaymentStatusAsync(int paymentId, string newStatus);
        Task<IEnumerable<PaymentMethodDto>> GetAllPaymentMethodsAsync();
        Task<PaymentMethodDto?> GetPaymentMethodByIdAsync(int id);
        Task<bool> CreatePaymentMethodAsync(PaymentMethodDto methodDto);
        Task<bool> UpdatePaymentMethodAsync(int id, PaymentMethodDto methodDto);
        Task<bool> DeletePaymentMethodAsync(int id);
    }
}