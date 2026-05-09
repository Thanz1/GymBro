using GymBro.Application.Orders;
using GymBro.Application.Payments;
using GymBro.Application.Shared;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Orders
{
    public class OrderAdminService : IOrderAdminService
    {
        private readonly GymBroDbContext _context;
        private readonly IPaymentAuditService _paymentAuditService;
        private readonly IOrderInventoryService _orderInventoryService;

        public OrderAdminService(
            GymBroDbContext context,
            IPaymentAuditService paymentAuditService,
            IOrderInventoryService orderInventoryService)
        {
            _context = context;
            _paymentAuditService = paymentAuditService;
            _orderInventoryService = orderInventoryService;
        }

        public async Task<OrderAdminPageResult> GetPageAsync(OrderAdminPageQuery query)
        {
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;

            var ordersQuery = _context.Orders
                .AsNoTracking()
                .Include(order => order.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.SearchString))
            {
                var keyword = query.SearchString.Trim();
                if (int.TryParse(keyword, out var orderId))
                {
                    ordersQuery = ordersQuery.Where(order =>
                        order.Id == orderId
                        || (order.User != null
                            && (order.User.FullName.Contains(keyword)
                                || order.User.Username.Contains(keyword))));
                }
                else
                {
                    ordersQuery = ordersQuery.Where(order =>
                        order.User != null
                        && (order.User.FullName.Contains(keyword)
                            || order.User.Username.Contains(keyword)));
                }
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var matchingStatuses = OrderStatus.GetEquivalentStatuses(query.Status);
                ordersQuery = ordersQuery.Where(order => matchingStatuses.Contains(order.Status));
            }

            ordersQuery = ordersQuery.OrderByDescending(order => order.OrderDate);

            var totalItems = await ordersQuery.CountAsync();
            var totalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling((double)totalItems / pageSize);

            if (totalPages > 0 && pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }

            var items = await ordersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new OrderAdminPageResult
            {
                Items = items,
                StatusOptions = OrderStatus.GetAllStatuses(),
                SearchString = query.SearchString,
                CurrentStatus = string.IsNullOrWhiteSpace(query.Status)
                    ? null
                    : OrderStatus.Normalize(query.Status),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<Order?> GetDetailsAsync(int id)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(order => order.User)
                .Include(order => order.OrderDetails)
                    .ThenInclude(detail => detail.Product)
                .Include(order => order.Payments)
                .FirstOrDefaultAsync(order => order.Id == id);
        }

        public async Task<OrderAdminEditData?> GetEditDataAsync(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(item => item.User)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (order == null)
            {
                return null;
            }

            order.Status = OrderStatus.Normalize(order.Status);

            return new OrderAdminEditData
            {
                Order = order,
                EditableStatuses = OrderStatus.GetEditableStatuses(order.Status)
            };
        }

        public async Task<Order?> GetDeleteModelAsync(int id)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(order => order.User)
                .FirstOrDefaultAsync(order => order.Id == id);
        }

        public async Task<OrderAdminCommandResult> UpdateStatusAsync(UpdateOrderStatusRequest request)
        {
            var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var normalizedStatus = OrderStatus.Normalize(request.Status);

            if (string.IsNullOrWhiteSpace(normalizedStatus))
            {
                AddError(errors, nameof(request.Status), "Trang thai khong duoc de trong.");
            }
            else if (!OrderStatus.IsKnownStatus(normalizedStatus))
            {
                AddError(errors, nameof(request.Status), "Trang thai don hang khong hop le.");
            }

            var existingOrder = await _context.Orders
                .Include(order => order.User)
                .Include(order => order.OrderDetails)
                .Include(order => order.Payments)
                .FirstOrDefaultAsync(order => order.Id == request.Id);

            if (existingOrder == null)
            {
                return OrderAdminCommandResult.NotFoundResult("Khong tim thay don hang can cap nhat.");
            }

            var calculatedTotal = existingOrder.OrderDetails.Sum(item => item.Quantity * item.Price);
            if (calculatedTotal <= 0)
            {
                AddError(errors, string.Empty, "Don hang phai co it nhat 1 san pham hop le.");
            }

            if (errors.Count > 0)
            {
                return OrderAdminCommandResult.Failure(ToReadOnly(errors));
            }

            var oldCancelled = OrderStatus.IsCancelled(existingOrder.Status);
            var newCancelled = OrderStatus.IsCancelled(normalizedStatus);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (newCancelled && !oldCancelled)
                {
                    var stockResult = await _orderInventoryService.ReleaseForOrderAsync(
                        existingOrder,
                        $"Hoan kho do admin cap nhat don hang #{existingOrder.Id} sang trang thai huy.");

                    if (!stockResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        AddError(
                            errors,
                            nameof(request.Status),
                            stockResult.Message ?? "Khong the cap nhat ton kho khi huy don.");

                        return OrderAdminCommandResult.Failure(ToReadOnly(errors));
                    }
                }
                else if (oldCancelled && !newCancelled)
                {
                    var stockResult = await _orderInventoryService.ReserveForOrderAsync(
                        existingOrder,
                        $"Xuat kho khi admin khoi phuc don hang #{existingOrder.Id}.");

                    if (!stockResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        AddError(
                            errors,
                            nameof(request.Status),
                            stockResult.Message ?? "Khong du ton kho de khoi phuc don hang.");

                        return OrderAdminCommandResult.Failure(ToReadOnly(errors));
                    }
                }

                existingOrder.Status = normalizedStatus;
                existingOrder.TotalAmount = calculatedTotal;

                var latestPayment = existingOrder.Payments
                    .OrderByDescending(payment => payment.Id)
                    .FirstOrDefault();

                if (latestPayment != null)
                {
                    latestPayment.Amount = calculatedTotal;
                    await SyncLatestPaymentAsync(
                        latestPayment,
                        oldCancelled,
                        newCancelled,
                        ResolveAuditActorName(request.AdminUsername));
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return OrderAdminCommandResult.Success("Da cap nhat don hang thanh cong.");
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();

                if (!await _context.Orders.AnyAsync(item => item.Id == request.Id))
                {
                    return OrderAdminCommandResult.NotFoundResult("Don hang khong con ton tai.");
                }

                AddError(errors, string.Empty, "Don hang vua duoc cap nhat o noi khac. Vui long tai lai va thu lai.");
                return OrderAdminCommandResult.Failure(ToReadOnly(errors));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                AddError(errors, string.Empty, "Co loi xay ra khi cap nhat don hang: " + ex.Message);
                return OrderAdminCommandResult.Failure(ToReadOnly(errors));
            }
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(item => item.OrderDetails)
                    .Include(item => item.Payments)
                    .FirstOrDefaultAsync(item => item.Id == id);

                if (order == null)
                {
                    return OperationResult.NotFoundResult("Khong tim thay don hang can xoa.");
                }

                if (!OrderStatus.IsCancelled(order.Status))
                {
                    return OperationResult.Failure(
                        "Chi co the xoa don hang da huy. Voi don dang hoat dong, hay cap nhat trang thai thay vi xoa.");
                }

                if (await _context.InventoryTransactions.AnyAsync(item => item.OrderId == id))
                {
                    return OperationResult.Failure(
                        "Khong the xoa don hang da co giao dich kho. Hay giu lai de bao toan lich su.");
                }

                _context.OrderDetails.RemoveRange(order.OrderDetails);
                _context.Payments.RemoveRange(order.Payments);
                _context.Orders.Remove(order);

                await _context.SaveChangesAsync();
                return OperationResult.Success("Da xoa don hang thanh cong.");
            }
            catch
            {
                return OperationResult.Failure("Co loi xay ra khi xoa don hang. Vui long thu lai sau.");
            }
        }

        private async Task SyncLatestPaymentAsync(
            Payment payment,
            bool oldCancelled,
            bool newCancelled,
            string auditActorName)
        {
            var actionedAt = DateTime.Now;

            if (newCancelled && !PaymentStatus.IsPaid(payment.Status))
            {
                if (!PaymentStatus.Equals(payment.Status, PaymentStatus.Cancelled))
                {
                    await _paymentAuditService.EnsureInitialLogAsync(payment);
                    payment.Status = PaymentStatus.Cancelled;
                    payment.PaymentDate = actionedAt;

                    await _paymentAuditService.LogAsync(
                        payment,
                        PaymentAuditAction.OrderCancelled,
                        auditActorName,
                        $"Admin '{auditActorName}' da cap nhat don hang sang trang thai huy.",
                        actionedAt);
                }

                return;
            }

            if (oldCancelled && !newCancelled && PaymentStatus.IsUnsuccessful(payment.Status))
            {
                await _paymentAuditService.EnsureInitialLogAsync(payment);
                payment.Status = ResolveRecoveryPaymentStatus(payment.PaymentMethod);
                payment.PaymentDate = actionedAt;

                await _paymentAuditService.LogAsync(
                    payment,
                    PaymentAuditAction.OrderRestored,
                    auditActorName,
                    $"Admin '{auditActorName}' da khoi phuc don hang ve trang thai hoat dong.",
                    actionedAt);
            }
        }

        private static string ResolveRecoveryPaymentStatus(string? paymentMethod)
        {
            return string.Equals(paymentMethod, "COD", StringComparison.OrdinalIgnoreCase)
                ? PaymentStatus.CashCollectionPending
                : PaymentStatus.AwaitingPayment;
        }

        private static string ResolveAuditActorName(string? adminUsername)
        {
            return string.IsNullOrWhiteSpace(adminUsername) ? "Admin" : adminUsername.Trim();
        }

        private static void AddError(
            IDictionary<string, List<string>> errors,
            string key,
            string message)
        {
            if (!errors.TryGetValue(key, out var values))
            {
                values = [];
                errors[key] = values;
            }

            values.Add(message);
        }

        private static Dictionary<string, string[]> ToReadOnly(
            IDictionary<string, List<string>> errors)
        {
            return errors.ToDictionary(
                item => item.Key,
                item => item.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
