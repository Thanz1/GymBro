using GymBro.Contracts;
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class OrderDetailsController : BaseAdminController
    {
        private readonly IOrderService _orderService;

        public OrderDetailsController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<IActionResult> Index(int orderId)
        {
            // Gọi API Order (Port 7003) để lấy chi tiết hóa đơn
            var details = await _orderService.GetOrderDetailsByOrderIdAsync(orderId);

            if (details == null) return NotFound();

            ViewBag.OrderId = orderId;
            return View(details);
        }
    }
}