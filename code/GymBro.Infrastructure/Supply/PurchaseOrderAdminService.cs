using GymBro.Application.Supply;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Supply
{
    public class PurchaseOrderAdminService : IPurchaseOrderAdminService
    {
        private readonly GymBroDbContext _context;

        public PurchaseOrderAdminService(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<PurchaseOrder>> GetListAsync()
        {
            return await _context.PurchaseOrders
                .AsNoTracking()
                .Include(order => order.Supplier)
                .OrderByDescending(order => order.OrderDate)
                .ToListAsync();
        }

        public async Task<PurchaseOrderCreateData> GetCreateDataAsync(PurchaseOrderCreateRequest? draft = null)
        {
            var orderDate = draft == null || draft.OrderDate == default
                ? DateTime.Now
                : draft.OrderDate;

            return new PurchaseOrderCreateData
            {
                SupplierId = draft?.SupplierId ?? 0,
                OrderDate = orderDate,
                SupplierOptions = await GetSupplierOptionsAsync()
            };
        }

        public async Task<PurchaseOrderCommandResult> CreateAsync(PurchaseOrderCreateRequest request)
        {
            var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (request.SupplierId <= 0)
            {
                AddError(errors, nameof(request.SupplierId), "Vui lòng chọn nhà cung cấp.");
            }

            var supplierExists = request.SupplierId > 0
                && await _context.Suppliers.AnyAsync(supplier =>
                    supplier.Id == request.SupplierId && supplier.IsActive);

            if (request.SupplierId > 0 && !supplierExists)
            {
                AddError(errors, nameof(request.SupplierId), "Nhà cung cấp không hợp lệ hoặc đã ngừng hoạt động.");
            }

            if (errors.Count > 0)
            {
                return PurchaseOrderCommandResult.Failure(ToReadOnly(errors));
            }

            var purchaseOrder = new PurchaseOrder
            {
                SupplierId = request.SupplierId,
                OrderDate = request.OrderDate == default ? DateTime.Now : request.OrderDate,
                Status = PurchaseOrderStatuses.Pending,
                TotalAmount = 0
            };

            _context.PurchaseOrders.Add(purchaseOrder);
            await _context.SaveChangesAsync();

            return PurchaseOrderCommandResult.Success(purchaseOrder, "Đã tạo phiếu nhập kho.");
        }

        public async Task<PurchaseOrderEditData?> GetEditDataAsync(int id)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .AsNoTracking()
                .Include(order => order.Supplier)
                .Include(order => order.PurchaseOrderDetails)
                    .ThenInclude(detail => detail.Product)
                .FirstOrDefaultAsync(order => order.Id == id);

            if (purchaseOrder == null)
            {
                return null;
            }

            return new PurchaseOrderEditData
            {
                PurchaseOrder = purchaseOrder,
                ProductOptions = await GetProductOptionsAsync()
            };
        }

        public async Task<PurchaseOrder?> GetDeleteDataAsync(int id)
        {
            return await _context.PurchaseOrders
                .AsNoTracking()
                .Include(order => order.Supplier)
                .FirstOrDefaultAsync(order => order.Id == id);
        }

        public async Task<PurchaseOrderActionResult> AddDetailAsync(PurchaseOrderDetailRequest request)
        {
            var purchaseOrder = await _context.PurchaseOrders.FindAsync(request.PurchaseOrderId);
            if (purchaseOrder == null)
            {
                return PurchaseOrderActionResult.NotFoundResult("Không tìm thấy phiếu nhập kho.", request.PurchaseOrderId);
            }

            if (PurchaseOrderStatuses.IsImported(purchaseOrder.Status))
            {
                return PurchaseOrderActionResult.Failure(request.PurchaseOrderId, "Không thể chỉnh sửa phiếu đã nhập kho.");
            }

            if (request.Quantity <= 0 || request.UnitPrice < 0)
            {
                return PurchaseOrderActionResult.Failure(request.PurchaseOrderId, "Số lượng và đơn giá không hợp lệ.");
            }

            var productExists = await _context.Products.AnyAsync(product => product.Id == request.ProductId);
            if (!productExists)
            {
                return PurchaseOrderActionResult.Failure(request.PurchaseOrderId, "Sản phẩm không tồn tại.");
            }

            var detail = new PurchaseOrderDetail
            {
                PurchaseOrderId = request.PurchaseOrderId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice
            };

            _context.PurchaseOrderDetails.Add(detail);
            purchaseOrder.TotalAmount += request.Quantity * request.UnitPrice;

            await _context.SaveChangesAsync();

            return PurchaseOrderActionResult.Success(request.PurchaseOrderId);
        }

        public async Task<PurchaseOrderActionResult> DeleteDetailAsync(int detailId)
        {
            var detail = await _context.PurchaseOrderDetails.FindAsync(detailId);
            if (detail == null)
            {
                return PurchaseOrderActionResult.NotFoundResult("Không tìm thấy dòng chi tiết cần xóa.");
            }

            var purchaseOrder = await _context.PurchaseOrders.FindAsync(detail.PurchaseOrderId);
            if (purchaseOrder == null)
            {
                return PurchaseOrderActionResult.NotFoundResult("Không tìm thấy phiếu nhập kho.", detail.PurchaseOrderId);
            }

            if (PurchaseOrderStatuses.IsImported(purchaseOrder.Status))
            {
                return PurchaseOrderActionResult.Failure(purchaseOrder.Id, "Không thể xóa chi tiết của phiếu đã nhập kho.");
            }

            purchaseOrder.TotalAmount -= detail.Quantity * detail.UnitPrice;
            if (purchaseOrder.TotalAmount < 0)
            {
                purchaseOrder.TotalAmount = 0;
            }

            _context.PurchaseOrderDetails.Remove(detail);
            await _context.SaveChangesAsync();

            return PurchaseOrderActionResult.Success(purchaseOrder.Id);
        }

        public async Task<PurchaseOrderActionResult> DeleteAsync(int id)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(order => order.PurchaseOrderDetails)
                .FirstOrDefaultAsync(order => order.Id == id);

            if (purchaseOrder == null)
            {
                return PurchaseOrderActionResult.NotFoundResult("Không tìm thấy phiếu nhập kho cần xóa.");
            }

            if (PurchaseOrderStatuses.IsImported(purchaseOrder.Status))
            {
                return PurchaseOrderActionResult.Failure(id, "Không thể xóa phiếu đã nhập kho.");
            }

            _context.PurchaseOrderDetails.RemoveRange(purchaseOrder.PurchaseOrderDetails);
            _context.PurchaseOrders.Remove(purchaseOrder);
            await _context.SaveChangesAsync();

            return PurchaseOrderActionResult.Success(message: "Đã xóa phiếu nhập hàng thành công.");
        }

        public async Task<PurchaseOrderActionResult> ApproveAsync(int id)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(order => order.PurchaseOrderDetails)
                .FirstOrDefaultAsync(order => order.Id == id);

            if (purchaseOrder == null)
            {
                return PurchaseOrderActionResult.NotFoundResult("Không tìm thấy phiếu nhập kho.", id);
            }

            if (PurchaseOrderStatuses.IsImported(purchaseOrder.Status))
            {
                return PurchaseOrderActionResult.Failure(id, "Phiếu này đã được nhập kho trước đó.");
            }

            if (!purchaseOrder.PurchaseOrderDetails.Any())
            {
                return PurchaseOrderActionResult.Failure(id, "Phiếu nhập chưa có sản phẩm nào để duyệt.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var productIds = purchaseOrder.PurchaseOrderDetails
                    .Select(detail => detail.ProductId)
                    .Distinct()
                    .ToList();

                var products = await _context.Products
                    .Where(product => productIds.Contains(product.Id))
                    .ToDictionaryAsync(product => product.Id);

                foreach (var detail in purchaseOrder.PurchaseOrderDetails)
                {
                    if (!products.TryGetValue(detail.ProductId, out var product))
                    {
                        throw new InvalidOperationException($"Không tìm thấy sản phẩm có ID {detail.ProductId}.");
                    }

                    product.StockQuantity += detail.Quantity;

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = detail.ProductId,
                        QuantityChange = detail.Quantity,
                        CreatedDate = DateTime.Now,
                        TransactionType = InventoryTransactionTypes.PurchaseImport,
                        PurchaseOrderId = purchaseOrder.Id,
                        Note = $"Nhập kho theo phiếu #{purchaseOrder.Id}"
                    });
                }

                purchaseOrder.Status = PurchaseOrderStatuses.Imported;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return PurchaseOrderActionResult.Success(id, "Đã duyệt phiếu nhập kho thành công.");
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync();
                return PurchaseOrderActionResult.Failure(id, "Lỗi khi duyệt phiếu nhập kho: " + exception.Message);
            }
        }

        private async Task<IReadOnlyList<LookupOptionDto>> GetSupplierOptionsAsync()
        {
            return await _context.Suppliers
                .AsNoTracking()
                .Where(supplier => supplier.IsActive)
                .OrderBy(supplier => supplier.SupplierName)
                .Select(supplier => new LookupOptionDto
                {
                    Id = supplier.Id,
                    Name = supplier.SupplierName
                })
                .ToListAsync();
        }

        private async Task<IReadOnlyList<LookupOptionDto>> GetProductOptionsAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .OrderBy(product => product.ProductName)
                .Select(product => new LookupOptionDto
                {
                    Id = product.Id,
                    Name = product.ProductName
                })
                .ToListAsync();
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
