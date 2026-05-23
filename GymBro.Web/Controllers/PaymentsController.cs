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
        public async Task<IActionResult> Details(int id)
        {
            // Gọi Service để tìm chi tiết thanh toán theo ID (Ví dụ ID = 8)
            // Lưu ý: Tên hàm GetPaymentByIdAsync có thể thay đổi tùy vào cách bạn đặt trong IPaymentService
            var payment = await _paymentService.GetPaymentByIdAsync(id);

            // Nếu không tìm thấy, báo lỗi 404
            if (payment == null)
            {
                return NotFound("Không tìm thấy thông tin giao dịch này.");
            }

            // Ném cục dữ liệu PaymentDto ra cho file Details.cshtml hiển thị
            return View(payment);
        }
    }
}