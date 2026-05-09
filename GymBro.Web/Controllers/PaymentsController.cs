using GymBro.Contracts;
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;

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
            // Gọi API thông qua Service thay vì truy vấn trực tiếp DB
            var payments = await _paymentService.GetAllPaymentsAsync();

            // Sắp xếp giảm dần theo ngày thanh toán ngay tại tầng Web
            var sortedPayments = payments.OrderByDescending(p => p.PaymentDate).ToList();

            return View(sortedPayments);
        }
    }
}