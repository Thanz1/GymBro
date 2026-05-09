using GymBro.Application.Payments;
using GymBro.Core;

namespace GymBro.Web.Features.Orders
{
    public static class OrderPresentationFactory
    {
        public static OrderListItemViewModel CreateListItem(Order order)
        {
            var payment = order.Payments.OrderByDescending(item => item.Id).FirstOrDefault();

            return new OrderListItemViewModel
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                CustomerDisplayName = order.User == null
                    ? "Unknown"
                    : string.IsNullOrWhiteSpace(order.User.FullName)
                        ? order.User.Username
                        : order.User.FullName,
                CustomerUsername = order.User?.Username ?? string.Empty,
                OrderStatus = CreateOrderStatusBadge(order.Status),
                Payment = payment == null ? null : CreatePaymentSummary(order, payment)
            };
        }

        public static OrderReadOnlyViewModel CreateReadOnly(Order order, Payment? latestPayment = null)
        {
            var payment = latestPayment ?? order.Payments.OrderByDescending(item => item.Id).FirstOrDefault();
            var customer = order.User;

            return new OrderReadOnlyViewModel
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                OrderStatus = CreateOrderStatusBadge(order.Status),
                CustomerDisplayName = customer == null
                    ? "Khach vang lai"
                    : string.IsNullOrWhiteSpace(customer.FullName)
                        ? customer.Username
                        : customer.FullName,
                CustomerUsername = customer?.Username ?? string.Empty,
                CustomerEmail = customer == null || string.IsNullOrWhiteSpace(customer.Email)
                    ? "Chua cap nhat"
                    : customer.Email,
                CustomerAddress = customer == null || string.IsNullOrWhiteSpace(customer.Address)
                    ? "Chua cap nhat"
                    : customer.Address,
                Payment = payment == null ? null : CreatePaymentSummary(order, payment),
                Items = order.OrderDetails
                    .Select(CreateLineItem)
                    .ToList()
            };
        }

        public static OrderDetailIndexViewModel CreateDetailIndex(
            int? orderId,
            IReadOnlyList<OrderDetail> items)
        {
            return new OrderDetailIndexViewModel
            {
                OrderId = orderId,
                Items = items.Select(CreateDetailListItem).ToList()
            };
        }

        public static OrderDetailReadOnlyViewModel CreateDetailReadOnly(OrderDetail detail)
        {
            return new OrderDetailReadOnlyViewModel
            {
                Id = detail.Id,
                OrderId = detail.OrderId,
                ProductName = detail.Product?.ProductName ?? "San pham da xoa",
                Quantity = detail.Quantity,
                Price = detail.Price,
                TotalAmount = detail.Price * detail.Quantity,
                OrderStatus = CreateOrderStatusBadge(detail.Order?.Status)
            };
        }

        private static OrderDetailListItemViewModel CreateDetailListItem(OrderDetail detail)
        {
            return new OrderDetailListItemViewModel
            {
                Id = detail.Id,
                OrderId = detail.OrderId,
                ProductName = detail.Product?.ProductName ?? "San pham da xoa",
                Quantity = detail.Quantity,
                Price = detail.Price,
                TotalAmount = detail.Price * detail.Quantity,
                OrderStatus = CreateOrderStatusBadge(detail.Order?.Status)
            };
        }

        private static OrderLineItemViewModel CreateLineItem(OrderDetail detail)
        {
            return new OrderLineItemViewModel
            {
                ProductId = detail.ProductId,
                ProductName = detail.Product?.ProductName ?? "San pham da xoa",
                Quantity = detail.Quantity,
                Price = detail.Price,
                TotalAmount = detail.Price * detail.Quantity
            };
        }

        private static OrderPaymentSummaryViewModel CreatePaymentSummary(Order order, Payment payment)
        {
            return new OrderPaymentSummaryViewModel
            {
                Method = payment.PaymentMethod,
                Status = CreatePaymentStatusBadge(payment.Status),
                UpdatedAt = payment.PaymentDate,
                Amount = payment.Amount,
                CanOpenPayment = CanOpenPayment(order, payment),
                PaymentActionText = GetPaymentActionText(payment)
            };
        }

        public static BadgeViewModel CreateOrderStatusBadge(string? status)
        {
            if (OrderStatus.IsCancelled(status))
            {
                return new BadgeViewModel { Text = status ?? string.Empty, CssClass = "bg-danger" };
            }

            if (OrderStatus.Equals(status, OrderStatus.Pending)
                || OrderStatus.Equals(status, OrderStatus.PendingPaymentVerification))
            {
                return new BadgeViewModel { Text = status ?? string.Empty, CssClass = "bg-warning text-dark" };
            }

            if (OrderStatus.Equals(status, OrderStatus.Processing)
                || OrderStatus.Equals(status, OrderStatus.Confirmed))
            {
                return new BadgeViewModel { Text = status ?? string.Empty, CssClass = "bg-primary" };
            }

            if (OrderStatus.IsShipping(status))
            {
                return new BadgeViewModel { Text = status ?? string.Empty, CssClass = "bg-info text-dark" };
            }

            if (OrderStatus.IsCompleted(status))
            {
                return new BadgeViewModel { Text = status ?? string.Empty, CssClass = "bg-success" };
            }

            return new BadgeViewModel { Text = status ?? string.Empty, CssClass = "bg-secondary" };
        }

        public static BadgeViewModel CreatePaymentStatusBadge(string? status)
        {
            if (PaymentStatus.IsPaid(status))
            {
                return new BadgeViewModel { Text = status ?? string.Empty, CssClass = "bg-success" };
            }

            if (PaymentStatus.Equals(status, PaymentStatus.AwaitingPayment)
                || PaymentStatus.Equals(status, PaymentStatus.PendingVerification))
            {
                return new BadgeViewModel { Text = status ?? string.Empty, CssClass = "bg-warning text-dark" };
            }

            if (PaymentStatus.Equals(status, PaymentStatus.CashCollectionPending))
            {
                return new BadgeViewModel { Text = status ?? string.Empty, CssClass = "bg-info text-dark" };
            }

            if (PaymentStatus.IsUnsuccessful(status))
            {
                return new BadgeViewModel { Text = status ?? string.Empty, CssClass = "bg-danger" };
            }

            return new BadgeViewModel { Text = status ?? string.Empty, CssClass = "bg-secondary" };
        }

        private static bool CanOpenPayment(Order order, Payment payment)
        {
            if (OrderStatus.IsCancelled(order.Status))
            {
                return false;
            }

            return PaymentStatus.Equals(payment.Status, PaymentStatus.AwaitingPayment)
                || PaymentStatus.Equals(payment.Status, PaymentStatus.PendingVerification);
        }

        private static string GetPaymentActionText(Payment payment)
        {
            return PaymentStatus.Equals(payment.Status, PaymentStatus.PendingVerification)
                ? "Xem thanh toan"
                : "Thanh toan";
        }
    }
}
