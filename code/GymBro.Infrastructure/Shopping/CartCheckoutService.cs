using GymBro.Application.Orders;
using GymBro.Application.Payments;
using GymBro.Application.Shopping;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text;

namespace GymBro.Infrastructure.Shopping
{
    public class CartCheckoutService : ICartCheckoutService
    {
        private readonly GymBroDbContext _context;
        private readonly IOrderInventoryService _orderInventoryService;
        private readonly IPaymentAuditService _paymentAuditService;
        private readonly IConfiguration _configuration;

        public CartCheckoutService(
            GymBroDbContext context,
            IOrderInventoryService orderInventoryService,
            IPaymentAuditService paymentAuditService,
            IConfiguration configuration)
        {
            _context = context;
            _orderInventoryService = orderInventoryService;
            _paymentAuditService = paymentAuditService;
            _configuration = configuration;
        }

        public async Task<CartMutationResult> AddToCartAsync(
            IReadOnlyCollection<CartItemData> currentCart,
            int productId,
            int quantity)
        {
            var cart = currentCart.ToList();

            if (quantity <= 0)
            {
                quantity = 1;
            }

            if (quantity > 100)
            {
                return CartMutationResult.Failure(
                    cart,
                    "So luong khong duoc vuot qua 100 san pham.");
            }

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == productId);

            if (product == null || product.Price <= 0)
            {
                return CartMutationResult.Failure(
                    cart,
                    "San pham khong ton tai hoac hien khong kha dung.");
            }

            if (product.StockQuantity <= 0)
            {
                return CartMutationResult.Failure(
                    cart,
                    $"San pham '{product.ProductName}' hien da het hang.");
            }

            var existingItem = cart.FirstOrDefault(item => item.ProductId == productId);
            if (existingItem != null)
            {
                var newQuantity = existingItem.Quantity + quantity;
                if (newQuantity > 100)
                {
                    return CartMutationResult.Failure(
                        cart,
                        "Tong so luong khong duoc vuot qua 100 san pham.");
                }

                if (newQuantity > product.StockQuantity)
                {
                    return CartMutationResult.Failure(
                        cart,
                        $"San pham nay chi con {product.StockQuantity} cai trong kho.");
                }

                cart[cart.IndexOf(existingItem)] = BuildCartItem(product, newQuantity);
            }
            else
            {
                if (quantity > product.StockQuantity)
                {
                    return CartMutationResult.Failure(
                        cart,
                        $"San pham nay chi con {product.StockQuantity} cai trong kho.");
                }

                cart.Add(BuildCartItem(product, quantity));
            }

            return CartMutationResult.Success(cart, "Da them san pham vao gio hang.");
        }

        public async Task<CartMutationResult> UpdateCartAsync(
            IReadOnlyCollection<CartItemData> currentCart,
            int productId,
            int quantity)
        {
            var cart = currentCart.ToList();
            var item = cart.FirstOrDefault(entry => entry.ProductId == productId);

            if (item == null)
            {
                return CartMutationResult.Failure(
                    cart,
                    "Khong tim thay san pham trong gio hang.");
            }

            if (quantity <= 0)
            {
                cart.Remove(item);
                return CartMutationResult.Success(cart, "Da xoa san pham khoi gio hang.");
            }

            if (quantity > 100)
            {
                return CartMutationResult.Failure(
                    cart,
                    "So luong khong duoc vuot qua 100 san pham.");
            }

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(entry => entry.Id == productId);

            if (product == null || product.Price <= 0)
            {
                cart.Remove(item);
                return CartMutationResult.Warning(
                    cart,
                    "San pham khong con kha dung va da bi xoa khoi gio hang.");
            }

            if (quantity > product.StockQuantity)
            {
                return CartMutationResult.Failure(
                    cart,
                    $"San pham nay chi con {product.StockQuantity} cai trong kho.");
            }

            cart[cart.IndexOf(item)] = BuildCartItem(product, quantity);
            return CartMutationResult.Success(cart, "Da cap nhat gio hang.");
        }

        public CartMutationResult RemoveFromCart(
            IReadOnlyCollection<CartItemData> currentCart,
            int productId)
        {
            var cart = currentCart.ToList();
            var item = cart.FirstOrDefault(entry => entry.ProductId == productId);

            if (item == null)
            {
                return CartMutationResult.Failure(
                    cart,
                    "Khong tim thay san pham trong gio hang.");
            }

            cart.Remove(item);
            return CartMutationResult.Success(cart, "Da xoa san pham khoi gio hang.");
        }

        public async Task<CartRefreshResult> RefreshCartAsync(
            IReadOnlyCollection<CartItemData> currentCart,
            bool enforceStock)
        {
            var refreshedItems = new List<CartItemData>();
            var messages = new List<string>();
            var hasChanges = false;

            foreach (var item in currentCart)
            {
                var product = await _context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(entry => entry.Id == item.ProductId);

                if (product == null || product.Price <= 0)
                {
                    hasChanges = true;
                    messages.Add($"San pham '{item.ProductName}' khong con kha dung va da bi xoa khoi gio.");
                    continue;
                }

                if (product.StockQuantity <= 0)
                {
                    hasChanges = true;
                    messages.Add($"San pham '{product.ProductName}' hien da het hang va da bi xoa khoi gio.");
                    continue;
                }

                var normalizedQuantity = item.Quantity < 1 ? 1 : item.Quantity;
                if (normalizedQuantity > 100)
                {
                    normalizedQuantity = 100;
                }

                if (enforceStock && normalizedQuantity > product.StockQuantity)
                {
                    normalizedQuantity = product.StockQuantity;
                }

                if (item.Quantity != normalizedQuantity
                    || item.Price != product.Price
                    || !string.Equals(item.ImageURL, product.ImageURL, StringComparison.Ordinal))
                {
                    hasChanges = true;
                }

                if (enforceStock && normalizedQuantity != item.Quantity)
                {
                    messages.Add($"So luong cua '{product.ProductName}' da duoc cap nhat theo ton kho hien tai.");
                }
                else if (item.Price != product.Price)
                {
                    messages.Add($"Gia cua '{product.ProductName}' da duoc cap nhat theo du lieu moi nhat.");
                }

                refreshedItems.Add(BuildCartItem(product, normalizedQuantity));
            }

            return new CartRefreshResult
            {
                Items = refreshedItems,
                HasChanges = hasChanges,
                Messages = messages.Distinct().ToList()
            };
        }

        public async Task<CheckoutPreparationResult> PrepareCheckoutAsync(
            IReadOnlyCollection<CartItemData> currentCart)
        {
            var refresh = await RefreshCartAsync(currentCart, enforceStock: true);

            var paymentMethods = await _context.PaymentMethods
                .AsNoTracking()
                .Where(method => method.IsActive)
                .OrderBy(method => method.Id)
                .Select(method => new CheckoutPaymentMethodOption
                {
                    Id = method.Id,
                    MethodName = method.MethodName,
                    Description = method.Description,
                    IsSupported = !IsUnsupportedPaymentMethod(method.MethodName),
                    IsBankTransfer = IsBankTransferMethod(method.MethodName)
                })
                .ToListAsync();

            return new CheckoutPreparationResult
            {
                Items = refresh.Items,
                PaymentMethods = paymentMethods,
                HasChanges = refresh.HasChanges,
                Messages = refresh.Messages,
                TotalPrice = refresh.Items.Sum(item => item.ThanhTien)
            };
        }

        public async Task<PlaceOrderResult> PlaceOrderAsync(
            int userId,
            IReadOnlyCollection<CartItemData> currentCart,
            int? paymentMethodId)
        {
            var checkout = await PrepareCheckoutAsync(currentCart);

            if (!checkout.Items.Any())
            {
                return PlaceOrderResult.Failure("Gio hang cua ban dang trong.");
            }

            if (checkout.HasChanges)
            {
                return PlaceOrderResult.CheckoutReview(
                    checkout,
                    "Thong tin gio hang da thay doi. Vui long kiem tra lai truoc khi dat hang.");
            }

            if (!paymentMethodId.HasValue)
            {
                return PlaceOrderResult.Failure("Vui long chon phuong thuc thanh toan.");
            }

            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(method => method.Id == paymentMethodId.Value && method.IsActive);

            if (paymentMethod == null)
            {
                return PlaceOrderResult.Failure("Phuong thuc thanh toan khong ton tai hoac da bi tat.");
            }

            if (IsUnsupportedPaymentMethod(paymentMethod.MethodName))
            {
                return PlaceOrderResult.Failure(
                    "Phuong thuc thanh toan nay dang tam thoi khong kha dung. Vui long chon phuong thuc khac.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pending
                };

                foreach (var item in checkout.Items)
                {
                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    });
                }

                order.TotalAmount = order.OrderDetails.Sum(detail => detail.Quantity * detail.Price);

                var payment = new Payment
                {
                    Order = order,
                    PaymentDate = DateTime.Now,
                    Amount = order.TotalAmount,
                    PaymentMethod = NormalizePaymentMethod(paymentMethod.MethodName),
                    Status = IsBankTransferMethod(paymentMethod.MethodName)
                        ? PaymentStatus.AwaitingPayment
                        : PaymentStatus.CashCollectionPending
                };

                _context.Orders.Add(order);
                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();

                var stockResult = await _orderInventoryService.ReserveForOrderAsync(
                    order,
                    $"Xuat kho cho don hang #{order.Id}");

                if (!stockResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return PlaceOrderResult.Failure(
                        stockResult.Message ?? "Khong the cap nhat ton kho khi dat hang.");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return PlaceOrderResult.Success(
                    order.Id,
                    IsBankTransferMethod(paymentMethod.MethodName),
                    IsBankTransferMethod(paymentMethod.MethodName)
                        ? "Don hang da duoc tao. Vui long hoan tat chuyen khoan de tiep tuc xu ly."
                        : "Dat hang thanh cong. Nhan vien se thu tien khi giao hang.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return PlaceOrderResult.Failure("Co loi xay ra khi dat hang: " + ex.Message);
            }
        }

        public async Task<PaymentPageResult> GetPaymentPageAsync(int userId, int orderId)
        {
            var order = await LoadOrderAsync(orderId, userId);
            if (order == null)
            {
                return PaymentPageResult.NotFoundResult();
            }

            var payment = order.Payments
                .OrderByDescending(item => item.Id)
                .FirstOrDefault();

            if (payment == null)
            {
                return PaymentPageResult.RedirectResult(
                    "Khong tim thay thong tin thanh toan cho don hang nay.");
            }

            string? noticeMessage = null;
            if (PaymentStatus.Equals(payment.Status, PaymentStatus.AwaitingPayment)
                && (DateTime.Now - order.OrderDate).TotalHours > 24)
            {
                noticeMessage = await CancelExpiredOrderAsync(order, payment);

                order = await LoadOrderAsync(orderId, userId);
                payment = order?.Payments.OrderByDescending(item => item.Id).FirstOrDefault();

                if (order == null || payment == null)
                {
                    return PaymentPageResult.RedirectResult(
                        "Khong tim thay thong tin thanh toan cho don hang nay.");
                }
            }

            var bankId = _configuration["VietQr:BankId"] ?? "MB";
            var accountNo = _configuration["VietQr:AccountNo"] ?? "0000123456789";
            var accountName = _configuration["VietQr:AccountName"] ?? "NGUYEN VAN A";
            var template = _configuration["VietQr:Template"] ?? "compact";
            var description = $"THANHTOAN DON {order.Id}";
            var qrImage = IsBankTransferMethod(payment.PaymentMethod)
                ? $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={order.TotalAmount:0}&addInfo={description}&accountName={Uri.EscapeDataString(accountName)}"
                : null;

            return PaymentPageResult.Success(
                order,
                payment.Status,
                qrImage,
                bankId,
                accountNo,
                accountName,
                description,
                noticeMessage,
                !string.IsNullOrWhiteSpace(noticeMessage));
        }

        public async Task<ConfirmOrderPaymentResult> ConfirmPaymentAsync(
            int userId,
            int orderId,
            string actorName)
        {
            var order = await _context.Orders
                .Include(item => item.OrderDetails)
                .Include(item => item.Payments)
                .FirstOrDefaultAsync(item => item.Id == orderId && item.UserId == userId);

            if (order == null)
            {
                return ConfirmOrderPaymentResult.NotFoundResult("Khong tim thay don hang.");
            }

            var payment = order.Payments
                .OrderByDescending(item => item.Id)
                .FirstOrDefault();

            if (payment == null)
            {
                return ConfirmOrderPaymentResult.RedirectToHistoryResult(
                    "Khong tim thay thong tin thanh toan cho don hang nay.");
            }

            if (PaymentStatus.IsPaid(payment.Status))
            {
                return ConfirmOrderPaymentResult.Info(
                    order.Id,
                    "Don hang nay da duoc thanh toan truoc do.");
            }

            if (PaymentStatus.Equals(payment.Status, PaymentStatus.PendingVerification))
            {
                return ConfirmOrderPaymentResult.Warning(
                    order.Id,
                    "Ban da xac nhan thanh toan roi. Vui long cho admin xu ly.");
            }

            if (PaymentStatus.IsUnsuccessful(payment.Status) || OrderStatus.IsCancelled(order.Status))
            {
                return ConfirmOrderPaymentResult.Failure(
                    order.Id,
                    "Don hang nay da bi huy hoac thanh toan that bai. Vui long lien he ho tro.");
            }

            if ((DateTime.Now - order.OrderDate).TotalHours > 24)
            {
                return ConfirmOrderPaymentResult.Failure(
                    order.Id,
                    await CancelExpiredOrderAsync(order, payment));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _paymentAuditService.EnsureInitialLogAsync(payment);

                var actionedAt = DateTime.Now;
                payment.Status = PaymentStatus.PendingVerification;
                payment.PaymentDate = actionedAt;

                if (OrderStatus.Equals(order.Status, OrderStatus.Pending))
                {
                    order.Status = OrderStatus.PendingPaymentVerification;
                }

                await _context.SaveChangesAsync();

                await _paymentAuditService.LogAsync(
                    payment,
                    PaymentAuditAction.CustomerConfirmed,
                    actorName,
                    $"Khach xac nhan chuyen khoan luc: {actionedAt:dd/MM/yyyy HH:mm:ss}",
                    actionedAt);

                await transaction.CommitAsync();

                return ConfirmOrderPaymentResult.Success(
                    order.Id,
                    "Cam on ban da xac nhan thanh toan. Don hang dang cho admin kiem tra va duyet.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ConfirmOrderPaymentResult.Failure(
                    order.Id,
                    "Co loi xay ra khi cap nhat thanh toan: " + ex.Message);
            }
        }

        private async Task<Order?> LoadOrderAsync(int orderId, int userId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(item => item.User)
                .Include(item => item.OrderDetails)
                    .ThenInclude(detail => detail.Product)
                .Include(item => item.Payments)
                .FirstOrDefaultAsync(item => item.Id == orderId && item.UserId == userId);
        }

        private async Task<string> CancelExpiredOrderAsync(Order order, Payment payment)
        {
            if (OrderStatus.IsCancelled(order.Status) || PaymentStatus.IsFinal(payment.Status))
            {
                return "Don hang nay da o trang thai ket thuc.";
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _paymentAuditService.EnsureInitialLogAsync(payment);

                var stockResult = await _orderInventoryService.ReleaseForOrderAsync(
                    order,
                    $"Hoan tra ton kho do huy don hang #{order.Id} (qua han thanh toan).");

                if (!stockResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return stockResult.Message ?? "Khong the cap nhat ton kho cho don hang qua han.";
                }

                var actionedAt = DateTime.Now;
                order.Status = OrderStatus.CancelledExpiredPayment;
                payment.Status = PaymentStatus.Cancelled;
                payment.PaymentDate = actionedAt;

                await _context.SaveChangesAsync();

                await _paymentAuditService.LogAsync(
                    payment,
                    PaymentAuditAction.OrderExpired,
                    "System",
                    "Huy do qua han 24 gio ke tu luc dat hang.",
                    actionedAt);

                await transaction.CommitAsync();
                return $"Don hang #{order.Id} da bi huy do qua han thanh toan 24 gio.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return "Co loi xay ra khi huy don hang qua han: " + ex.Message;
            }
        }

        private static CartItemData BuildCartItem(Product product, int quantity)
        {
            return new CartItemData
            {
                ProductId = product.Id,
                ProductName = product.ProductName,
                Price = product.Price,
                ImageURL = product.ImageURL,
                Quantity = quantity
            };
        }

        private static bool IsBankTransferMethod(string? methodName)
        {
            var normalized = NormalizeText(methodName);
            return normalized.Contains("qr", StringComparison.Ordinal)
                || normalized.Contains("ngan hang", StringComparison.Ordinal)
                || normalized.Contains("chuyen khoan", StringComparison.Ordinal)
                || string.Equals(normalized, "banking", StringComparison.Ordinal);
        }

        private static bool IsUnsupportedPaymentMethod(string? methodName)
        {
            var normalized = NormalizeText(methodName);
            return string.IsNullOrWhiteSpace(normalized)
                || normalized.Contains("vnpay", StringComparison.Ordinal)
                || normalized.Contains("momo", StringComparison.Ordinal);
        }

        private static string NormalizePaymentMethod(string? methodName)
        {
            return IsBankTransferMethod(methodName) ? "Banking" : "COD";
        }

        private static string NormalizeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder
                .ToString()
                .Normalize(NormalizationForm.FormC);
        }
    }
}
