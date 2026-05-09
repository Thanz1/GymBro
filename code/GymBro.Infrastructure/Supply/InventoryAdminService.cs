using GymBro.Application.Supply;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Supply
{
    public class InventoryAdminService : IInventoryAdminService
    {
        private readonly GymBroDbContext _context;

        public InventoryAdminService(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<InventoryOverviewResult> GetOverviewAsync(string? searchString)
        {
            var productsQuery = _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var normalizedSearch = searchString.Trim();
                productsQuery = productsQuery.Where(product =>
                    product.ProductName.Contains(normalizedSearch));
            }

            var products = await productsQuery
                .OrderBy(product => product.ProductName)
                .ToListAsync();

            return new InventoryOverviewResult
            {
                Products = products,
                SearchString = searchString
            };
        }

        public async Task<InventoryHistoryResult> GetHistoryAsync(int? productId)
        {
            var transactionsQuery = _context.InventoryTransactions
                .AsNoTracking()
                .Include(transaction => transaction.Product)
                .AsQueryable();

            var productName = "Tất cả sản phẩm";

            if (productId.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(transaction => transaction.ProductId == productId.Value);

                var product = await _context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == productId.Value);

                productName = product != null ? product.ProductName : $"Sản phẩm #{productId.Value}";
            }

            var transactions = await transactionsQuery
                .OrderByDescending(transaction => transaction.CreatedDate)
                .ToListAsync();

            return new InventoryHistoryResult
            {
                Transactions = transactions,
                ProductName = productName,
                ProductId = productId
            };
        }

        public async Task<InventoryAdjustData?> GetAdjustDataAsync(
            int productId,
            int? newQuantity = null,
            string? note = null)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == productId);

            if (product == null)
            {
                return null;
            }

            return new InventoryAdjustData
            {
                Product = product,
                NewQuantity = newQuantity ?? product.StockQuantity,
                Note = note
            };
        }

        public async Task<InventoryAdjustCommandResult> AdjustAsync(InventoryAdjustRequest request)
        {
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return InventoryAdjustCommandResult.NotFoundResult("Không tìm thấy sản phẩm cần điều chỉnh.");
            }

            var data = new InventoryAdjustData
            {
                Product = product,
                NewQuantity = request.NewQuantity,
                Note = request.Note
            };

            var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (request.NewQuantity < 0)
            {
                AddError(errors, nameof(request.NewQuantity), "Số lượng tồn kho không được âm.");
            }

            var quantityDifference = request.NewQuantity - product.StockQuantity;
            if (quantityDifference == 0)
            {
                AddError(errors, nameof(request.NewQuantity), "Số lượng mới trùng với số lượng hiện tại.");
            }

            if (errors.Count > 0)
            {
                return InventoryAdjustCommandResult.Failure(data, ToReadOnly(errors));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                product.StockQuantity = request.NewQuantity;

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = request.ProductId,
                    QuantityChange = quantityDifference,
                    Note = string.IsNullOrWhiteSpace(request.Note) ? "Kiểm kê kho" : request.Note.Trim(),
                    CreatedDate = DateTime.Now,
                    TransactionType = InventoryTransactionTypes.Adjustment
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return InventoryAdjustCommandResult.Success("Đã điều chỉnh tồn kho thành công.");
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync();
                AddError(errors, string.Empty, "Có lỗi xảy ra khi điều chỉnh kho: " + exception.Message);
                return InventoryAdjustCommandResult.Failure(data, ToReadOnly(errors));
            }
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

        private static IReadOnlyDictionary<string, string[]> ToReadOnly(
            IDictionary<string, List<string>> errors)
        {
            return errors.ToDictionary(
                item => item.Key,
                item => item.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
