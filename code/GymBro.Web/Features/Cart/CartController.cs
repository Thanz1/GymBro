using GymBro.Application.Account;
using GymBro.Application.Shopping;
using GymBro.Core;
using GymBro.Web.Features.Cart;
using GymBro.Web.Features.Orders;
using GymBro.Web.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "Cart";

        private readonly IAccountService _accountService;
        private readonly ICartCheckoutService _cartCheckoutService;

        public CartController(
            IAccountService accountService,
            ICartCheckoutService cartCheckoutService)
        {
            _accountService = accountService;
            _cartCheckoutService = cartCheckoutService;
        }

        private List<CartItemData> GetCart()
        {
            return HttpContext.Session.GetObject<List<CartItemData>>(CartSessionKey) ?? [];
        }

        private void SaveCart(IReadOnlyCollection<CartItemData> cart)
        {
            var items = cart.ToList();
            HttpContext.Session.SetObject(CartSessionKey, items);
            SetCartSummary(items);
        }

        private void SetCartSummary(IReadOnlyCollection<CartItemData> cart)
        {
            ViewData["TotalQuantity"] = cart.Sum(item => item.Quantity);
            ViewData["TotalPrice"] = cart.Sum(item => item.ThanhTien);
        }

        public async Task<IActionResult> Index()
        {
            var refresh = await _cartCheckoutService.RefreshCartAsync(GetCart(), enforceStock: false);
            if (refresh.HasChanges)
            {
                SaveCart(refresh.Items);
                SetCartMessages(refresh.Messages);
            }
            else
            {
                SetCartSummary(refresh.Items);
            }

            return View(refresh.Items);
        }

        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var result = await _cartCheckoutService.AddToCartAsync(GetCart(), productId, quantity);
            SaveCart(result.Items);
            SetCartMutationMessage(result);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCart(int id, int quantity)
        {
            var result = await _cartCheckoutService.UpdateCartAsync(GetCart(), id, quantity);
            SaveCart(result.Items);
            SetCartMutationMessage(result);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveFromCart(int id)
        {
            var result = _cartCheckoutService.RemoveFromCart(GetCart(), id);
            SaveCart(result.Items);
            SetCartMutationMessage(result);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Checkout()
        {
            var user = await GetActiveUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });
            }

            var checkout = await _cartCheckoutService.PrepareCheckoutAsync(GetCart());
            if (checkout.HasChanges)
            {
                SaveCart(checkout.Items);
                SetCartMessages(checkout.Messages);
            }
            else
            {
                SetCartSummary(checkout.Items);
            }

            if (!checkout.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống hoặc có sản phẩm không còn khả dụng.";
                return RedirectToAction(nameof(Index));
            }

            return View(new CartCheckoutViewModel
            {
                User = new CheckoutCustomerViewModel
                {
                    DisplayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName,
                    Email = user.Email,
                    Address = user.Address
                },
                Items = checkout.Items,
                PaymentMethods = checkout.PaymentMethods,
                TotalPrice = checkout.TotalPrice
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(int? paymentMethodId)
        {
            var user = await GetActiveUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _cartCheckoutService.PlaceOrderAsync(user.Id, GetCart(), paymentMethodId);
            if (result.RequiresCheckoutReview && result.Checkout != null)
            {
                SaveCart(result.Checkout.Items);
                SetCartMessages(result.Checkout.Messages, result.Message);
                return RedirectToAction(nameof(Checkout));
            }

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Checkout));
            }

            HttpContext.Session.Remove(CartSessionKey);

            if (result.RedirectToPayment && result.OrderId.HasValue)
            {
                return RedirectToAction(nameof(Payment), new { id = result.OrderId.Value });
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(OrderSuccessful), new { id = result.OrderId });
        }

        public async Task<IActionResult> Payment(int id)
        {
            var user = await GetActiveUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Payment/" + id });
            }

            var result = await _cartCheckoutService.GetPaymentPageAsync(user.Id, id);
            if (result.NotFound)
            {
                return NotFound();
            }

            if (result.RedirectToHistory)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("OrderHistory", "Account");
            }

            return View(new CartPaymentViewModel
            {
                Order = OrderPresentationFactory.CreateReadOnly(result.Order!),
                PaymentStatus = result.PaymentStatus,
                QrImage = result.QrImage,
                BankId = result.BankId,
                AccountNo = result.AccountNo,
                AccountName = result.AccountName,
                Description = result.Description,
                NoticeMessage = string.IsNullOrWhiteSpace(result.Message) ? null : result.Message,
                IsWarning = result.IsWarning
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var user = await GetActiveUserAsync();
            if (user == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiếp tục.";
                return RedirectToAction("Login", "Account");
            }

            var result = await _cartCheckoutService.ConfirmPaymentAsync(
                user.Id,
                id,
                GetPaymentAuditActorName(user));

            if (result.NotFound)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("Index", "Home");
            }

            if (result.RedirectToHistory)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("OrderHistory", "Account");
            }

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(OrderSuccessful), new { id = result.OrderId ?? id });
            }

            if (result.IsInfo)
            {
                TempData["InfoMessage"] = result.Message;
                return RedirectToAction(nameof(OrderSuccessful), new { id = result.OrderId ?? id });
            }

            if (result.IsWarning)
            {
                TempData["WarningMessage"] = result.Message;
                return RedirectToAction(nameof(OrderSuccessful), new { id = result.OrderId ?? id });
            }

            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Payment), new { id = result.OrderId ?? id });
        }

        public IActionResult OrderSuccessful(int? id = null)
        {
            return View(model: id);
        }

        private async Task<User?> GetActiveUserAsync()
        {
            var sessionUser = HttpContext.Session.GetObject<User>("User");
            if (sessionUser == null)
            {
                return null;
            }

            var user = await _accountService.GetActiveUserAsync(sessionUser.Id);
            if (user == null)
            {
                HttpContext.Session.Remove("User");
            }

            return user;
        }

        private void SetCartMutationMessage(CartMutationResult result)
        {
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = result.Message;
                return;
            }

            if (result.IsWarning)
            {
                TempData["WarningMessage"] = result.Message;
                return;
            }

            TempData["ErrorMessage"] = result.Message;
        }

        private void SetCartMessages(
            IReadOnlyList<string> messages,
            string? overrideWarning = null)
        {
            if (!string.IsNullOrWhiteSpace(overrideWarning))
            {
                TempData["WarningMessage"] = overrideWarning;
                return;
            }

            if (messages.Count > 0)
            {
                TempData["WarningMessage"] = string.Join(" ", messages.Distinct());
            }
        }

        private static string GetPaymentAuditActorName(User user)
        {
            return string.IsNullOrWhiteSpace(user.Username) ? "Khach hang" : user.Username;
        }
    }
}
