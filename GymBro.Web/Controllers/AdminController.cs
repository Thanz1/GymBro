using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class AdminController : BaseAdminController
    {
        private readonly GymBroDbContext _context;
        public AdminController(GymBroDbContext context) { _context = context; }

        public async Task<IActionResult> Dashboard()
        {
            // 1. Tổng doanh thu (Sửa: TrangThai -> Status, TongTien -> TotalAmount)
            ViewBag.TotalRevenue = await _context.Orders
                .Where(o => o.Status != "Đã hủy")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // 2. Đếm số liệu (Sửa: SoLuongTon -> StockQuantity)
            ViewBag.NewOrdersCount = await _context.Orders.CountAsync(o => o.Status == "Chờ xử lý");
            ViewBag.TotalMembersCount = await _context.Users.CountAsync(u => u.Role != "Admin");
            ViewBag.LowStockCount = await _context.Products.CountAsync(p => p.StockQuantity < 10);

            // 3. Đơn hàng gần đây (Sửa: NgayDat -> OrderDate)
            ViewBag.RecentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // 4. Biểu đồ doanh thu 7 ngày qua
            var sevenDaysAgo = DateTime.Today.AddDays(-6);
            var revenueData = await _context.Orders
                .Where(o => o.Status != "Đã hủy" && o.OrderDate >= sevenDaysAgo)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.TotalAmount) })
                .ToListAsync();

            // Xử lý dữ liệu biểu đồ cho View
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