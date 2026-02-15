using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GymBro.Core;
using GymBro.Infrastructure;

namespace GymBro.Web.Controllers
{
    public class OrdersController : BaseAdminController
    {
        private readonly GymBroDbContext _context;

        public OrdersController(GymBroDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DANH SÁCH ĐƠN HÀNG (INDEX)
        // ==========================================
        public async Task<IActionResult> Index(string status)
        {
            // Lấy danh sách đơn hàng kèm thông tin người dùng
            var query = _context.Orders.Include(o => o.User).AsQueryable();

            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrEmpty(status))
<<<<<<< HEAD
            {
                query = query.Where(o => o.Status == status);
            }

            // Tạo danh sách trạng thái để đưa vào Dropdown lọc
            var statusList = GetStatusList();
            ViewBag.Status = new SelectList(statusList, status); // Fix lỗi "no ViewData item of type..."

            // Sắp xếp đơn mới nhất lên đầu
            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            return View(orders);
=======
                // SỬA: TrangThai -> Status
                query = query.Where(o => o.Status == status);

            if (!string.IsNullOrEmpty(searchString))
                // Tìm theo ID đơn hoặc tên User
                query = query.Where(o => o.Id.ToString() == searchString || o.User.Username.Contains(searchString));

            ViewBag.CurrentStatus = status;

            // SỬA: NgayDat -> OrderDate
            return View(await query.OrderByDescending(o => o.OrderDate).ToListAsync());
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
        }

        // ==========================================
        // 2. XEM CHI TIẾT ĐƠN HÀNG (DETAILS)
        // ==========================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails) // Kèm chi tiết đơn hàng
                .ThenInclude(od => od.Product) // Kèm thông tin sản phẩm trong chi tiết
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // ==========================================
        // 3. SỬA TRẠNG THÁI (EDIT - GET)
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(x => x.Id == id);
            if (order == null) return NotFound();

            // Gửi danh sách trạng thái sang View để chọn
            ViewBag.Status = new SelectList(GetStatusList(), order.Status);

            return View(order);
        }

        // ==========================================
        // 4. LƯU TRẠNG THÁI (EDIT - POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Order order)
        {
            if (id != order.Id) return NotFound();

            // Lấy đơn hàng gốc từ DB lên để cập nhật an toàn
            var existingOrder = await _context.Orders.FindAsync(id);
            if (existingOrder == null) return NotFound();

            if (ModelState.IsValid)
            {
<<<<<<< HEAD
                try
                {
                    // Chỉ cập nhật trạng thái, giữ nguyên các thông tin khác (Tiền, Ngày đặt...)
                    existingOrder.Status = order.Status;

                    _context.Update(existingOrder);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
=======
                // SỬA: TrangThai -> Status
                existingOrder.Status = order.Status; // Cập nhật trạng thái
                await _context.SaveChangesAsync();
>>>>>>> b440fc362f63b696b48ce18ea8836d734d9ba595
            }

            // Nếu lỗi thì load lại dropdown để không bị crash View
            ViewBag.Status = new SelectList(GetStatusList(), order.Status);
            return View(order);
        }

        // ==========================================
        // 5. CÁC HÀM HỖ TRỢ (HELPER)
        // ==========================================

        // Hàm lấy danh sách trạng thái chuẩn (dùng chung cho cả Index và Edit)
        private List<string> GetStatusList()
        {
            return new List<string>
    {
        "Chờ xử lý",    // Pending
        "Đang xử lý",   // Processing
        "Đang giao",    // Shipped
        "Đã giao",      // Delivered
        "Đã hủy"        // Cancelled
    };
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}