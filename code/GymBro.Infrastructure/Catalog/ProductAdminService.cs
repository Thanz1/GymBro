using GymBro.Application.Catalog;
using GymBro.Application.Files;
using GymBro.Application.Shared;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Catalog
{
    public class ProductAdminService : IProductAdminService
    {
        private const long MaxImageSizeInBytes = 5 * 1024 * 1024;

        private readonly GymBroDbContext _context;
        private readonly IProductImageStorage _imageStorage;

        public ProductAdminService(
            GymBroDbContext context,
            IProductImageStorage imageStorage)
        {
            _context = context;
            _imageStorage = imageStorage;
        }

        public async Task<ProductAdminPageResult> GetPageAsync(ProductAdminPageQuery query)
        {
            var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;

            var productsQuery = _context.Products
                .Include(product => product.Category)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.SearchString))
            {
                var normalizedSearch = query.SearchString.Trim();
                productsQuery = productsQuery.Where(product =>
                    product.ProductName.Contains(normalizedSearch)
                    || (product.Description != null && product.Description.Contains(normalizedSearch)));
            }

            if (query.CategoryId.HasValue)
            {
                productsQuery = productsQuery.Where(product => product.CategoryId == query.CategoryId.Value);
            }

            productsQuery = productsQuery.OrderByDescending(product => product.Id);

            var totalItems = await productsQuery.CountAsync();
            var totalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling((double)totalItems / pageSize);

            if (totalPages > 0 && pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }

            var items = await productsQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new ProductAdminPageResult
            {
                Items = items,
                Categories = await GetCategoryOptionsAsync(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                SearchString = query.SearchString,
                CategoryId = query.CategoryId
            };
        }

        public async Task<IReadOnlyList<CategoryOptionDto>> GetCategoryOptionsAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.CategoryName)
                .Select(category => new CategoryOptionDto
                {
                    Id = category.Id,
                    Name = category.CategoryName
                })
                .ToListAsync();
        }

        public async Task<Product?> GetDetailsAsync(int id)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .FirstOrDefaultAsync(product => product.Id == id);
        }

        public async Task<Product?> GetEditModelAsync(int id)
        {
            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(product => product.Id == id);
        }

        public async Task<ProductCommandResult> CreateAsync(ProductUpsertRequest request)
        {
            var errors = await ValidateAsync(request);
            if (errors.Count > 0)
            {
                return ProductCommandResult.Failure(errors);
            }

            var product = new Product
            {
                ProductName = request.ProductName.Trim(),
                Description = NormalizeOptionalText(request.Description),
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                CategoryId = request.CategoryId
            };

            string? newImageFileName = null;
            try
            {
                newImageFileName = await _imageStorage.SaveAsync(request.ImageUpload);
                product.ImageURL = newImageFileName;

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return ProductCommandResult.Success(product, "Product created.");
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(newImageFileName))
                {
                    await _imageStorage.DeleteIfManagedAsync(newImageFileName);
                }

                throw;
            }
        }

        public async Task<ProductCommandResult> UpdateAsync(ProductUpsertRequest request)
        {
            if (!request.Id.HasValue)
            {
                return ProductCommandResult.NotFoundResult("Product not found.");
            }

            var existingProduct = await _context.Products.FindAsync(request.Id.Value);
            if (existingProduct == null)
            {
                return ProductCommandResult.NotFoundResult("Product not found.");
            }

            var errors = await ValidateAsync(request);
            if (errors.Count > 0)
            {
                return ProductCommandResult.Failure(errors);
            }

            var previousImageFileName = existingProduct.ImageURL;
            string? newImageFileName = null;

            try
            {
                if (request.ImageUpload?.HasContent == true)
                {
                    newImageFileName = await _imageStorage.SaveAsync(request.ImageUpload);
                }

                existingProduct.ProductName = request.ProductName.Trim();
                existingProduct.Description = NormalizeOptionalText(request.Description);
                existingProduct.Price = request.Price;
                existingProduct.StockQuantity = request.StockQuantity;
                existingProduct.CategoryId = request.CategoryId;

                if (!string.IsNullOrWhiteSpace(newImageFileName))
                {
                    existingProduct.ImageURL = newImageFileName;
                }

                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(newImageFileName))
                {
                    await _imageStorage.DeleteIfManagedAsync(previousImageFileName);
                }

                return ProductCommandResult.Success(existingProduct, "Product updated.");
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(newImageFileName))
                {
                    await _imageStorage.DeleteIfManagedAsync(newImageFileName);
                }

                throw;
            }
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return OperationResult.NotFoundResult("KhÃ´ng tÃ¬m tháº¥y sáº£n pháº©m cáº§n xÃ³a.");
            }

            if (await _context.OrderDetails.AnyAsync(orderDetail => orderDetail.ProductId == id))
            {
                return OperationResult.Failure("KhÃ´ng thá»ƒ xÃ³a sáº£n pháº©m nÃ y vÃ¬ Ä‘Ã£ cÃ³ trong Ä‘Æ¡n hÃ ng.");
            }

            if (await _context.Reviews.AnyAsync(review => review.ProductId == id))
            {
                return OperationResult.Failure("KhÃ´ng thá»ƒ xÃ³a sáº£n pháº©m nÃ y vÃ¬ Ä‘Ã£ cÃ³ Ä‘Ã¡nh giÃ¡.");
            }

            if (await _context.Wishlists.AnyAsync(wishlist => wishlist.ProductId == id))
            {
                return OperationResult.Failure("KhÃ´ng thá»ƒ xÃ³a sáº£n pháº©m nÃ y vÃ¬ Ä‘Ã£ cÃ³ trong danh sÃ¡ch yÃªu thÃ­ch.");
            }

            if (await _context.InventoryTransactions.AnyAsync(transaction => transaction.ProductId == id))
            {
                return OperationResult.Failure("KhÃ´ng thá»ƒ xÃ³a sáº£n pháº©m nÃ y vÃ¬ Ä‘Ã£ cÃ³ giao dá»‹ch kho.");
            }

            if (await _context.PurchaseOrderDetails.AnyAsync(detail => detail.ProductId == id))
            {
                return OperationResult.Failure("KhÃ´ng thá»ƒ xÃ³a sáº£n pháº©m nÃ y vÃ¬ Ä‘Ã£ cÃ³ trong phiáº¿u nháº­p.");
            }

            var imageFileName = product.ImageURL;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            await _imageStorage.DeleteIfManagedAsync(imageFileName);

            return OperationResult.Success("ÄÃ£ xÃ³a sáº£n pháº©m thÃ nh cÃ´ng.");
        }

        private async Task<Dictionary<string, string[]>> ValidateAsync(ProductUpsertRequest request)
        {
            var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(request.ProductName))
            {
                AddError(errors, nameof(request.ProductName), "TÃªn sáº£n pháº©m lÃ  báº¯t buá»™c.");
            }

            if (request.Price <= 0)
            {
                AddError(errors, nameof(request.Price), "GiÃ¡ sáº£n pháº©m pháº£i lá»›n hÆ¡n 0.");
            }

            if (request.StockQuantity < 0)
            {
                AddError(errors, nameof(request.StockQuantity), "Sá»‘ lÆ°á»£ng tá»“n kho khÃ´ng Ä‘Æ°á»£c Ã¢m.");
            }

            var categoryExists = await _context.Categories.AnyAsync(category => category.Id == request.CategoryId);
            if (!categoryExists)
            {
                AddError(errors, nameof(request.CategoryId), "Danh má»¥c khÃ´ng há»£p lá»‡.");
            }

            if (request.ImageUpload?.HasContent == true)
            {
                if (!request.ImageUpload.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    AddError(errors, "ImageURL", "Tá»‡p táº£i lÃªn pháº£i lÃ  hÃ¬nh áº£nh.");
                }

                if (request.ImageUpload.Length > MaxImageSizeInBytes)
                {
                    AddError(errors, "ImageURL", "áº¢nh táº£i lÃªn khÃ´ng Ä‘Æ°á»£c lá»›n hÆ¡n 5MB.");
                }
            }

            return errors.ToDictionary(
                item => item.Key,
                item => item.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
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

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
