using GymBro.Contracts;
using GymBro.Contracts;
using GymBro.Service;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class AdminController : BaseAdminController
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly IIdentityService _identityService;

        public AdminController(
            IOrderService orderService,
            IProductService productService,
            IIdentityService identityService)
        {
            _orderService = orderService;
            _productService = productService;
            _identityService = identityService;
        }

        public async Task<IActionResult> Dashboard()
        {
            // 1. Lấy dữ liệu tổng hợp từ các Service (API) thay vì DB
            // Lưu ý: Bạn cần bổ sung các hàm này vào Service (Xem bước 2)

            var allOrders = await _orderService.GetAllOrdersAsync();
            var allProducts = await _productService.GetAllProductsAsync();
            var allUsers = await _identityService.GetAllUsersAsync();

            // 2. Tính toán số liệu trên Web dựa trên DTO nhận được
            ViewBag.TotalRevenue = allOrders
                .Where(o => o.Status != "Đã hủy")
                .Sum(o => o.TotalAmount);

            ViewBag.NewOrdersCount = allOrders.Count(o => o.Status == "Chờ xử lý");
            ViewBag.TotalMembersCount = allUsers.Count(u => u.Role != "Admin");
            ViewBag.LowStockCount = allProducts.Count(p => p.StockQuantity < 10);

            // 3. Đơn hàng gần đây
            ViewBag.RecentOrders = allOrders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();

            // 4. Xử lý biểu đồ 7 ngày qua
            var sevenDaysAgo = DateTime.Today.AddDays(-6);
            var revenueData = allOrders
                .Where(o => o.Status != "Đã hủy" && o.OrderDate >= sevenDaysAgo)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.TotalAmount) })
                .ToList();

            ViewBag.ChartLabels = Enumerable.Range(0, 7)
                .Select(i => sevenDaysAgo.AddDays(i).ToString("dd/MM"))
                .ToArray();

            ViewBag.ChartData = Enumerable.Range(0, 7)
                .Select(i => revenueData.FirstOrDefault(r => r.Date == sevenDaysAgo.AddDays(i))?.Total ?? 0)
                .ToArray();

            return View();
        }
    }
}