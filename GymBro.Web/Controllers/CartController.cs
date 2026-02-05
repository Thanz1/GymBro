using GymBro.Web.DTOs; // Dùng chung DTO với API hoặc tạo ViewModel riêng cũng được
using GymBro.Core;
using GymBro.Infrastructure;
using GymBro.Web.Helpers; // Để dùng SessionExtensions
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

        // Lấy giỏ hàng từ Session
        private List<CartItemDto> GetCart()
        {
            var cart = HttpContext.Session.GetObject<List<CartItemDto>>("Cart");
            return cart ?? new List<CartItemDto>();
        }

        // Lưu giỏ hàng vào Session
        private void SaveCart(List<CartItemDto> cart)
        {
            HttpContext.Session.SetObject("Cart", cart);

            // Cập nhật số lượng hiển thị trên Menu
            ViewData["TotalQuantity"] = cart.Sum(item => item.Quantity);
            ViewData["TotalPrice"] = cart.Sum(item => item.ThanhTien);
        }

        // 1. HIỂN THỊ GIỎ HÀNG
        public IActionResult Index()
        {
            var cart = GetCart();
            ViewData["TotalQuantity"] = cart.Sum(item => item.Quantity);
            ViewData["TotalPrice"] = cart.Sum(item => item.ThanhTien);
            return View(cart);
        }

        // 2. THÊM VÀO GIỎ (Gọi từ nút "Mua ngay" hoặc "Thêm vào giỏ")
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

        // 3. CẬP NHẬT SỐ LƯỢNG
        [HttpPost]
        public IActionResult UpdateCart(int id, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                if (quantity > 0)
                {
                    item.Quantity = quantity;
                }
                else
                {
                    cart.Remove(item); // Nếu số lượng <= 0 thì xóa luôn
                }
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        // 4. XÓA KHỎI GIỎ
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

        // 5. TRANG THANH TOÁN (CHECKOUT)
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

            // Lấy danh sách phương thức thanh toán từ DB (nếu có bảng PaymentMethod)
            // Hoặc tạo cứng list demo
            ViewBag.PaymentMethods = await _context.PaymentMethods.ToListAsync();

            return View(cart);
        }

        // 6. XỬ LÝ ĐẶT HÀNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(int paymentMethodId)
        {
            var user = HttpContext.Session.GetObject<User>("User");
            if (user == null) return RedirectToAction("Login", "Account");

            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");

            // Tạo đơn hàng mới
            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                Status = "Chờ thanh toán",
                TotalAmount = cart.Sum(c => c.ThanhTien)
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Lưu để lấy OrderId

            // Lưu chi tiết đơn hàng
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
            await _context.SaveChangesAsync();

            // Xóa giỏ hàng sau khi đặt thành công
            HttpContext.Session.Remove("Cart");

            // Nếu chọn chuyển khoản (QR Code) -> Chuyển sang trang Payment
            // Giả sử paymentMethodId = 2 là chuyển khoản
            if (paymentMethodId == 2)
            {
                return RedirectToAction("Payment", new { id = order.Id });
            }

            return RedirectToAction("OrderSuccessful");
        }

        // 7. TRANG THANH TOÁN QR (Payment)
        public async Task<IActionResult> Payment(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Tạo link QR Code (VietQR)
            string bankId = "MB"; // Ví dụ MB Bank
            string accountNo = "0000123456789"; // Số tài khoản của bạn
            string accountName = "NGUYEN VAN A"; // Tên chủ TK
            string content = $"THANHTOAN DONHANG {order.Id}";

            string qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact.png?amount={order.TotalAmount}&addInfo={content}&accountName={accountName}";

            ViewBag.QRImage = qrUrl;
            ViewBag.BankId = bankId;
            ViewBag.AccountNo = accountNo;
            ViewBag.AccountName = accountName;
            ViewBag.Description = content;
            ViewBag.PaymentStatus = order.Status; // Truyền trạng thái sang View

            return View(order);
        }

        // 8. XÁC NHẬN ĐÃ CHUYỂN KHOẢN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = "Chờ xác minh"; // Chuyển trạng thái

            // Tạo bản ghi thanh toán
            var payment = new Payment
            {
                OrderId = order.Id,
                PaymentDate = DateTime.Now,
                Amount = order.TotalAmount,
                PaymentMethod = "Banking",
                Status = "Chờ xác minh"
            };
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            return RedirectToAction("Payment", new { id = id });
        }

        // 9. ĐẶT HÀNG THÀNH CÔNG
        public IActionResult OrderSuccessful()
        {
            return View();
        }
    }
}