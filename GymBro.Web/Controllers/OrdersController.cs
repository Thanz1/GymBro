using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using GymBro.Contracts;
using GymBro.Service;

namespace GymBro.Web.Controllers
{
    public class OrdersController : BaseAdminController
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // 1. Danh sách hóa đơn
        public async Task<IActionResult> Index(string status)
        {
            // Gọi API lấy toàn bộ hóa đơn
            var orders = await _orderService.GetAllOrdersAsync();

            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.Status == status);
            }

            ViewBag.Status = new SelectList(GetStatusList(), status);

            return View(orders.OrderByDescending(o => o.OrderDate).ToList());
        }

        // 2. Chi tiết hóa đơn
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Gọi API lấy thông tin hóa đơn kèm chi tiết (OrderDetails)
            var order = await _orderService.GetOrderByIdAsync(id.Value);

            if (order == null) return NotFound();

            return View(order);
        }

        // 3. Giao diện chỉnh sửa trạng thái (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var order = await _orderService.GetOrderByIdAsync(id.Value);
            if (order == null) return NotFound();

            ViewBag.Status = new SelectList(GetStatusList(), order.Status);

            return View(order);
        }

        // 4. Cập nhật trạng thái hóa đơn (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrderDto orderDto)
        {
            if (id != orderDto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                // Chỉ gửi yêu cầu cập nhật trạng thái sang API
                var success = await _orderService.UpdateOrderStatusAsync(id, orderDto.Status);

                if (success)
                {
                    TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Không thể cập nhật trạng thái đơn hàng.");
            }

            ViewBag.Status = new SelectList(GetStatusList(), orderDto.Status);
            return View(orderDto);
        }

        private List<string> GetStatusList()
        {
            return new List<string> { "Chờ xử lý", "Đang xử lý", "Đang giao", "Đã giao", "Đã hủy" };
        }
    }
}