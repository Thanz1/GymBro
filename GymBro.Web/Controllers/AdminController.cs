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
            // 1. Tổng doanh thu (Chỉ tính đơn đã hoàn thành/thanh toán)
            ViewBag.TotalRevenue = await _context.Orders
                .Where(o => o.TrangThai != "Đã hủy")
                .SumAsync(o => (decimal?)o.TongTien) ?? 0;

            // 2. Đếm số liệu
            ViewBag.NewOrdersCount = await _context.Orders.CountAsync(o => o.TrangThai == "Chờ xử lý");
            ViewBag.TotalMembersCount = await _context.Users.CountAsync(u => u.Role != "Admin");
            ViewBag.LowStockCount = await _context.Products.CountAsync(p => p.SoLuongTon < 10);

            // 3. Đơn hàng gần đây
            ViewBag.RecentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.NgayDat)
                .Take(5)
                .ToListAsync();

            // 4. Biểu đồ doanh thu 7 ngày qua
            var sevenDaysAgo = DateTime.Today.AddDays(-6);
            var revenueData = await _context.Orders
                .Where(o => o.TrangThai != "Đã hủy" && o.NgayDat >= sevenDaysAgo)
                .GroupBy(o => o.NgayDat.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.TongTien) })
                .ToListAsync();

            // Xử lý dữ liệu biểu đồ cho View
            ViewBag.ChartLabels = Enumerable.Range(0, 7).Select(i => sevenDaysAgo.AddDays(i).ToString("dd/MM")).ToArray();
            ViewBag.ChartData = Enumerable.Range(0, 7).Select(i =>
                revenueData.FirstOrDefault(r => r.Date == sevenDaysAgo.AddDays(i))?.Total ?? 0).ToArray();

            return View();
        }
    }
}