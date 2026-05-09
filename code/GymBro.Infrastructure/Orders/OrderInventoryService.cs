using GymBro.Application.Orders;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Orders
{
    public class OrderInventoryService : IOrderInventoryService
    {
        private readonly GymBroDbContext _context;

        public OrderInventoryService(GymBroDbContext context)
        {
            _context = context;
        }

        public Task<StockAdjustmentResult> ReserveForOrderAsync(Order order, string note)
        {
            return AdjustReservationAsync(order, reserveStock: true, note);
        }

        public Task<StockAdjustmentResult> ReleaseForOrderAsync(Order order, string note)
        {
            return AdjustReservationAsync(order, reserveStock: false, note);
        }

        private async Task<StockAdjustmentResult> AdjustReservationAsync(Order order, bool reserveStock, string note)
        {
            if (order == null)
            {
                return StockAdjustmentResult.Failure("KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n hÃ ng.");
            }

            await EnsureOrderDetailsLoadedAsync(order);

            if (order.OrderDetails == null || !order.OrderDetails.Any())
            {
                return StockAdjustmentResult.Failure("ÄÆ¡n hÃ ng khÃ´ng cÃ³ sáº£n pháº©m Ä‘á»ƒ cáº­p nháº­t tá»“n kho.");
            }

            var currentNetChanges = await _context.InventoryTransactions
                .Where(transaction => transaction.OrderId == order.Id)
                .GroupBy(transaction => transaction.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    NetChange = group.Sum(transaction => transaction.QuantityChange)
                })
                .ToDictionaryAsync(item => item.ProductId, item => item.NetChange);

            var expectedQuantities = order.OrderDetails
                .GroupBy(detail => detail.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(detail => detail.Quantity)
                })
                .ToList();

            var pendingAdjustments = new List<PendingStockAdjustment>();

            foreach (var item in expectedQuantities)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                {
                    return StockAdjustmentResult.Failure("KhÃ´ng tÃ¬m tháº¥y sáº£n pháº©m Ä‘á»ƒ cáº­p nháº­t tá»“n kho.");
                }

                var currentNet = currentNetChanges.GetValueOrDefault(item.ProductId);
                var targetNet = reserveStock ? -item.Quantity : 0;
                var delta = targetNet - currentNet;

                if (delta == 0)
                {
                    continue;
                }

                if (delta < 0 && product.StockQuantity < Math.Abs(delta))
                {
                    return StockAdjustmentResult.Failure(
                        $"KhÃ´ng Ä‘á»§ hÃ ng trong kho cho sáº£n pháº©m '{product.ProductName}'.");
                }

                pendingAdjustments.Add(new PendingStockAdjustment(product, delta));
            }

            foreach (var adjustment in pendingAdjustments)
            {
                adjustment.Product.StockQuantity += adjustment.Delta;

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = adjustment.Product.Id,
                    QuantityChange = adjustment.Delta,
                    TransactionType = adjustment.Delta < 0 ? "BÃ¡n hÃ ng" : "HoÃ n tráº£",
                    OrderId = order.Id,
                    Note = note,
                    CreatedDate = DateTime.Now
                });
            }

            return StockAdjustmentResult.Success(pendingAdjustments.Count > 0);
        }

        private async Task EnsureOrderDetailsLoadedAsync(Order order)
        {
            var collection = _context.Entry(order).Collection(item => item.OrderDetails);
            if (!collection.IsLoaded)
            {
                await collection.LoadAsync();
            }
        }

        private sealed record PendingStockAdjustment(Product Product, int Delta);
    }
}
