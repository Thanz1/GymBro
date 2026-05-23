using GymBro.Contracts;
using GymBro.Service;
using GymBro.Web.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly IConfiguration _configuration;

        public CartController(
            IProductService productService,
            IOrderService orderService,
            IConfiguration configuration)
        {
            _productService = productService;
            _orderService = orderService;
            _configuration = configuration;
        }

        private List<CartItemDto> GetCart() =>
            HttpContext.Session.GetObject<List<CartItemDto>>("Cart") ?? new List<CartItemDto>();

        private void SaveCart(List<CartItemDto> cart) =>
            HttpContext.Session.SetObject("Cart", cart);

        private UserDto? GetSessionUser() =>
            HttpContext.Session.GetObject<UserDto>("User");

        public IActionResult Index()
        {
            var cart = GetCart();
            ViewData["TotalPrice"] = cart.Sum(item => item.ThanhTien);
            return View(cart);
        }

        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null) return NotFound();

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(x => x.ProductId == productId);

            if (existingItem != null)
                existingItem.Quantity += quantity;
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
            TempData["SuccessMessage"] = "Đã thêm vào giỏ hàng!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult UpdateCart(int id, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == id);
            if (item != null)
            {
                if (quantity <= 0)
                    cart.Remove(item);
                else
                    item.Quantity = quantity;
            }

            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveFromCart(int id)
        {
            var cart = GetCart();
            cart.RemoveAll(x => x.ProductId == id);
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            var user = GetSessionUser();
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });

            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.TotalPrice = cart.Sum(x => x.ThanhTien);
            return View(new CheckoutDto { CartItems = cart, UserId = user.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(int paymentMethodId)
        {
            var user = GetSessionUser();
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });

            var cart = GetCart();
            if (!cart.Any())
                return RedirectToAction(nameof(Index));

            var checkoutDto = new CheckoutDto
            {
                UserId = user.Id,
                CartItems = cart,
                PaymentMethodId = paymentMethodId,
                OrderDate = DateTime.Now
            };

            var result = await _orderService.PlaceOrderAsync(checkoutDto);

            if (result == null)
            {
                TempData["ErrorMessage"] = "Đặt hàng thất bại. Kiểm tra Order.API đang chạy (port 5003/7003).";
                return RedirectToAction(nameof(Checkout));
            }

            HttpContext.Session.Remove("Cart");

            if (result.RequiresPayment)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Payment), new { id = result.OrderId });
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(OrderSuccessful), new { id = result.OrderId });
        }

        public async Task<IActionResult> Payment(int id)
        {
            var user = GetSessionUser();
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = $"/Cart/Payment/{id}" });

            var order = await _orderService.GetOrderDetailsAsync(id);
            if (order == null || order.UserId != user.Id)
                return NotFound();

            var bankId = _configuration["BankTransfer:BankId"] ?? "970422";
            var accountNo = _configuration["BankTransfer:AccountNo"] ?? "0123456789";
            var accountName = _configuration["BankTransfer:AccountName"] ?? "GYMBRO";
            var amount = (long)order.TotalAmount;
            var description = $"GymBro{order.Id}";

            ViewBag.PaymentStatus = "Chờ thanh toán";
            ViewBag.QRImage = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={amount}&addInfo={description}&accountName={Uri.EscapeDataString(accountName)}";
            ViewBag.AccountNo = accountNo;
            ViewBag.AccountName = accountName;
            ViewBag.BankId = bankId;

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var user = GetSessionUser();
            if (user == null)
                return RedirectToAction("Login", "Account");

            var success = await _orderService.ConfirmPaymentAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã ghi nhận chuyển khoản. Đơn hàng chờ xác minh.";
                return RedirectToAction(nameof(OrderSuccessful), new { id });
            }

            TempData["ErrorMessage"] = "Không thể xác nhận thanh toán.";
            return RedirectToAction(nameof(Payment), new { id });
        }

        public IActionResult OrderSuccessful(int? id = null)
        {
            ViewBag.OrderId = id;
            return View();
        }
    }
}
