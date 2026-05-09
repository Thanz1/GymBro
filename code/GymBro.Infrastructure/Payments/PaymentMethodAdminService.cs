using GymBro.Application.Payments;
using GymBro.Application.Shared;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Payments
{
    public class PaymentMethodAdminService : IPaymentMethodAdminService
    {
        private readonly GymBroDbContext _context;

        public PaymentMethodAdminService(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<PaymentMethod>> GetListAsync()
        {
            return await _context.PaymentMethods
                .AsNoTracking()
                .OrderByDescending(method => method.IsActive)
                .ThenBy(method => method.MethodName)
                .ToListAsync();
        }

        public async Task<PaymentMethod?> GetByIdAsync(int id)
        {
            return await _context.PaymentMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(method => method.Id == id);
        }

        public async Task<PaymentMethodCommandResult> CreateAsync(PaymentMethodUpsertRequest request)
        {
            var errors = await ValidateAsync(request);
            if (errors.Count > 0)
            {
                return PaymentMethodCommandResult.Failure(errors);
            }

            var paymentMethod = new PaymentMethod
            {
                MethodName = request.MethodName.Trim(),
                Description = NormalizeOptionalText(request.Description),
                IsActive = request.IsActive
            };

            _context.PaymentMethods.Add(paymentMethod);
            await _context.SaveChangesAsync();

            return PaymentMethodCommandResult.Success(paymentMethod, "Thêm phương thức thanh toán thành công.");
        }

        public async Task<PaymentMethodCommandResult> UpdateAsync(PaymentMethodUpsertRequest request)
        {
            if (!request.Id.HasValue)
            {
                return PaymentMethodCommandResult.NotFoundResult("Không tìm thấy phương thức thanh toán cần cập nhật.");
            }

            var paymentMethod = await _context.PaymentMethods.FindAsync(request.Id.Value);
            if (paymentMethod == null)
            {
                return PaymentMethodCommandResult.NotFoundResult("Không tìm thấy phương thức thanh toán cần cập nhật.");
            }

            var errors = await ValidateAsync(request);
            if (errors.Count > 0)
            {
                return PaymentMethodCommandResult.Failure(errors);
            }

            paymentMethod.MethodName = request.MethodName.Trim();
            paymentMethod.Description = NormalizeOptionalText(request.Description);
            paymentMethod.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return PaymentMethodCommandResult.Success(paymentMethod, "Cập nhật phương thức thanh toán thành công.");
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            var paymentMethod = await _context.PaymentMethods.FindAsync(id);
            if (paymentMethod == null)
            {
                return OperationResult.NotFoundResult("Không tìm thấy phương thức thanh toán cần xóa.");
            }

            if (await HasPaymentsUsingMethodAsync(paymentMethod.MethodName))
            {
                return OperationResult.Failure("Không thể xóa phương thức này vì đã có giao dịch sử dụng nó. Hãy vô hiệu hóa thay vì xóa.");
            }

            _context.PaymentMethods.Remove(paymentMethod);
            await _context.SaveChangesAsync();

            return OperationResult.Success("Xóa phương thức thanh toán thành công.");
        }

        private async Task<Dictionary<string, string[]>> ValidateAsync(PaymentMethodUpsertRequest request)
        {
            var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(request.MethodName))
            {
                AddError(errors, nameof(request.MethodName), "Tên phương thức không được để trống.");
                return ToReadOnly(errors);
            }

            var normalizedName = request.MethodName.Trim();

            var duplicateExists = await _context.PaymentMethods.AnyAsync(method =>
                method.Id != (request.Id ?? 0)
                && method.MethodName.ToLower() == normalizedName.ToLower());

            if (duplicateExists)
            {
                AddError(errors, nameof(request.MethodName), "Tên phương thức thanh toán đã tồn tại.");
            }

            return ToReadOnly(errors);
        }

        private async Task<bool> HasPaymentsUsingMethodAsync(string methodName)
        {
            var normalizedStoredMethod = NormalizeStoredPaymentMethod(methodName);
            return await _context.Payments.AnyAsync(payment => payment.PaymentMethod == normalizedStoredMethod);
        }

        private static string NormalizeStoredPaymentMethod(string? methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                return string.Empty;
            }

            if (IsBankTransferMethod(methodName))
            {
                return "Banking";
            }

            if (IsCashOnDeliveryMethod(methodName))
            {
                return "COD";
            }

            return methodName.Trim();
        }

        private static bool IsBankTransferMethod(string? methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                return false;
            }

            return methodName.Contains("QR", StringComparison.OrdinalIgnoreCase)
                || methodName.Contains("Ngân hàng", StringComparison.OrdinalIgnoreCase)
                || methodName.Contains("Chuyển khoản", StringComparison.OrdinalIgnoreCase)
                || string.Equals(methodName, "Banking", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCashOnDeliveryMethod(string? methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                return false;
            }

            return methodName.Contains("COD", StringComparison.OrdinalIgnoreCase)
                || methodName.Contains("nhận hàng", StringComparison.OrdinalIgnoreCase)
                || methodName.Contains("thu tiền", StringComparison.OrdinalIgnoreCase)
                || string.Equals(methodName, "CashOnDelivery", StringComparison.OrdinalIgnoreCase);
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

        private static Dictionary<string, string[]> ToReadOnly(
            IDictionary<string, List<string>> errors)
        {
            return errors.ToDictionary(
                item => item.Key,
                item => item.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
