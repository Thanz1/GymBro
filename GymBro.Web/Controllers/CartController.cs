using GymBro.Web.DTOs;
using GymBro.Core;
using GymBro.Infrastructure;
using GymBro.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly GymBroDbContext _context;

        public CartController(GymBroDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // CÁC HÀM HỖ TRỢ GIỎ HÀNG (PRIVATE)
        // ==========================================
        private List<CartItemDto> GetCart()
        {
            var cart = HttpContext.Session.GetObject<List<CartItemDto>>("Cart");
            return cart ?? new List<CartItemDto>();
        }

        private void SaveCart(List<CartItemDto> cart)
        {
            HttpContext.Session.SetObject("Cart", cart);
            ViewData["TotalQuantity"] = cart.Sum(item => item.Quantity);
            ViewData["TotalPrice"] = cart.Sum(item => item.ThanhTien);
        }

        // ==========================================
        // 1. QUẢN LÝ GIỎ HÀNG (INDEX, ADD, UPDATE, REMOVE)
        // ==========================================
        public IActionResult Index()
        {
            var cart = GetCart();
            ViewData["TotalQuantity"] = cart.Sum(item => item.Quantity);
            ViewData["TotalPrice"] = cart.Sum(item => item.ThanhTien);
            return View(cart);
        }

        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(x => x.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItemDto
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    ImageURL = product.ImageURL,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateCart(int id, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                if (quantity > 0) item.Quantity = quantity;
                else cart.Remove(item);

                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        public IActionResult RemoveFromCart(int id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // 2. CHECKOUT & ĐẶT HÀNG (QUAN TRỌNG)
        // ==========================================
        public async Task<IActionResult> Checkout()
        {
            var user = HttpContext.Session.GetObject<User>("User");
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });
            }

            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");

            ViewBag.TotalPrice = cart.Sum(i => i.ThanhTien);

            // Giả lập danh sách phương thức nếu DB chưa có
            // 1: COD, 2: Banking
            var methods = await _context.PaymentMethods.ToListAsync();
            if (!methods.Any())
            {
                // Nếu DB trống, tạo list tạm để view không bị lỗi
                ViewBag.PaymentMethods = new List<dynamic> {
                    new { Id = 1, MethodName = "Thanh toán khi nhận hàng (COD)" },
                    new { Id = 2, MethodName = "Chuyển khoản Ngân hàng (QR Code)" }
                };
            }
            else
            {
                ViewBag.PaymentMethods = methods;
            }

            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(int paymentMethodId)
        {
            var user = HttpContext.Session.GetObject<User>("User");
            if (user == null) return RedirectToAction("Login", "Account");

            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");

            // 1. Tạo đơn hàng (Order)
            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                Status = "Chờ xử lý", // Trạng thái mặc định của đơn hàng
                TotalAmount = cart.Sum(c => c.ThanhTien)
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Lưu để lấy OrderId

            // 2. Lưu chi tiết đơn hàng (OrderDetails)
            foreach (var item in cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };
                _context.OrderDetails.Add(orderDetail);
            }

            // 3. QUAN TRỌNG: TẠO BẢN GHI THANH TOÁN (PAYMENT) NGAY LẬP TỨC
            // Xác định tên phương thức
            string methodString = (paymentMethodId == 2) ? "Banking" : "COD";

            var initialPayment = new Payment
            {
                OrderId = order.Id,
                PaymentDate = DateTime.Now,
                Amount = order.TotalAmount,
                PaymentMethod = methodString,
                // Nếu là COD thì coi như xong bước thanh toán (chờ thu tiền)
                // Nếu là Banking thì là "Chờ thanh toán"
                Status = (paymentMethodId == 1) ? "Chờ thu tiền" : "Chờ thanh toán"
            };

            _context.Payments.Add(initialPayment);
            await _context.SaveChangesAsync(); // Lưu tất cả xuống DB

            // 4. Dọn dẹp giỏ hàng
            HttpContext.Session.Remove("Cart");

            // 5. Điều hướng
            if (paymentMethodId == 2) // Nếu chọn Banking -> Sang trang quét mã
            {
                return RedirectToAction("Payment", new { id = order.Id });
            }

            // Nếu COD -> Thông báo thành công
            return RedirectToAction("OrderSuccessful");
        }

        // ==========================================
        // 3. THANH TOÁN QR CODE
        // ==========================================
        public async Task<IActionResult> Payment(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Cấu hình VietQR
            string bankId = "MB";
            string accountNo = "0000123456789"; // Thay số tài khoản của bạn
            string accountName = "NGUYEN VAN A"; // Thay tên của bạn
            string content = $"THANHTOAN DON {order.Id}";

            string qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact.png?amount={order.TotalAmount}&addInfo={content}&accountName={accountName}";

            ViewBag.QRImage = qrUrl;
            ViewBag.BankId = bankId;
            ViewBag.AccountNo = accountNo;
            ViewBag.AccountName = accountName;
            ViewBag.Description = content;
            ViewBag.PaymentStatus = order.Status;

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            // Cập nhật trạng thái Đơn hàng
            order.Status = "Chờ xác minh"; // Báo cho Admin biết khách đã bấm "Tôi đã chuyển khoản"
            _context.Update(order);

            // Cập nhật trạng thái Thanh toán (Payment)
            // Thay vì tạo mới, ta tìm cái cũ đã tạo ở bước PlaceOrder để update
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == id);

            if (payment != null)
            {
                payment.Status = "Chờ xác minh";
                payment.PaymentDate = DateTime.Now; // Cập nhật lại thời gian bấm xác nhận
                _context.Update(payment);
            }
            else
            {
                // Trường hợp dự phòng (nếu lỡ database cũ chưa có payment)
                var newPayment = new Payment
                {
                    OrderId = order.Id,
                    PaymentDate = DateTime.Now,
                    Amount = order.TotalAmount,
                    PaymentMethod = "Banking",
                    Status = "Chờ xác minh"
                };
                _context.Payments.Add(newPayment);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã gửi xác nhận thanh toán. Vui lòng đợi Admin kiểm tra!";
            return RedirectToAction("Payment", new { id = id });
        }

        public IActionResult OrderSuccessful()
        {
            return View();
        }
    }
}