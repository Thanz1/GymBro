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

        public CartController(IProductService productService, IOrderService orderService)
        {
            _productService = productService;
            _orderService = orderService;
        }

        private List<CartItemDto> GetCart()
        {
            return HttpContext.Session.GetObject<List<CartItemDto>>("Cart") ?? new List<CartItemDto>();
        }

        private void SaveCart(List<CartItemDto> cart)
        {
            HttpContext.Session.SetObject("Cart", cart);
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            // Đảm bảo CartItemDto có thuộc tính này hoặc logic tính toán tương ứng
            ViewBag.TotalPrice = cart.Sum(item => item.Price * item.Quantity);
            return View(cart);
        }

        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null) return NotFound();

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(x => x.ProductId == productId);

            if (existingItem != null) existingItem.Quantity += quantity;
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
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(int paymentMethodId)
        {
            var cart = GetCart();
            if (cart == null || !cart.Any()) return RedirectToAction("Index");

            // 1. Lấy UserId từ Claims (Mặc định là string)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // 2. Kiểm tra nếu chưa đăng nhập
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Index" });
            }

            // 3. Chuyển đổi an toàn từ string sang int
            // Giả sử CheckoutDto.UserId của bạn là kiểu int
            int userIdInt = int.Parse(userIdClaim);

            var checkoutDto = new CheckoutDto
            {
                UserId = userIdInt, // Gán int vào int -> Hết lỗi
                CartItems = cart,
                PaymentMethodId = paymentMethodId,
                OrderDate = DateTime.Now
            };

            var success = await _orderService.PlaceOrderAsync(checkoutDto);

            if (success)
            {
                HttpContext.Session.Remove("Cart");
                return RedirectToAction("OrderSuccessful");
            }

            ModelState.AddModelError("", "Đặt hàng thất bại, vui lòng thử lại.");
            return View("Index", cart);
        }
    }
}