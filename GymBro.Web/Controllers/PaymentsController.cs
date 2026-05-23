using GymBro.Contracts;
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace GymBro.Web.Controllers
{
    public class PaymentsController : BaseAdminController
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // Hiển thị danh sách lịch sử thanh toán
        public async Task<IActionResult> Index()
        {
            var payments = await _paymentService.GetAllPaymentsAsync();
            var sortedPayments = payments.OrderByDescending(p => p.PaymentDate).ToList();
            return View(sortedPayments);
        }

        // Hiển thị chi tiết thanh toán theo ID giao dịch thanh toán
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);

            if (payment == null)
            {
                return NotFound("Không tìm thấy thông tin giao dịch này.");
            }

            return View(payment);
        }

        // Xử lý nút bấm cập nhật trạng thái thanh toán từ Admin
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int paymentId, string newStatus)
        {
            var isSuccess = await _paymentService.UpdatePaymentStatusAsync(paymentId, newStatus);

            if (isSuccess)
            {
                TempData["SuccessMessage"] = "Đã cập nhật trạng thái giao dịch thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể cập nhật trạng thái. Vui lòng kiểm tra lại!";
            }

            return RedirectToAction("Details", new { id = paymentId });
        }
    }
}