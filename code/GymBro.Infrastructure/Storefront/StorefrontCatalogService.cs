using GymBro.Application.Storefront;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Storefront
{
    public class StorefrontCatalogService : IStorefrontCatalogService
    {
        public const int DefaultShopPageSize = 12;

        private readonly GymBroDbContext _context;

        public StorefrontCatalogService(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Product>> GetProductsAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .OrderByDescending(product => product.Id)
                .ToListAsync();
        }

        public async Task<StorefrontHomeData> GetHomeAsync()
        {
            var newProducts = await _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .OrderByDescending(product => product.Id)
                .Take(8)
                .ToListAsync();

            var bestSellingProducts = await _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Where(product => product.Id % 3 == 0)
                .OrderByDescending(product => product.Id)
                .Take(8)
                .ToListAsync();

            return new StorefrontHomeData
            {
                NewProducts = newProducts,
                BestSellingProducts = bestSellingProducts
            };
        }

        public async Task<StorefrontShopData> GetShopAsync(StorefrontShopQuery query)
        {
            var pageSize = query.PageSize <= 0 ? DefaultShopPageSize : query.PageSize;
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;

            var productsQuery = _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var normalizedKeyword = query.Keyword.Trim();
                productsQuery = productsQuery.Where(product =>
                    product.ProductName.Contains(normalizedKeyword)
                    || (product.Description != null && product.Description.Contains(normalizedKeyword)));
            }

            if (query.CategoryId.HasValue)
            {
                productsQuery = productsQuery.Where(product => product.CategoryId == query.CategoryId.Value);
            }

            if (query.MinPrice.HasValue)
            {
                productsQuery = productsQuery.Where(product => product.Price >= query.MinPrice.Value);
            }

            if (query.MaxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(product => product.Price <= query.MaxPrice.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Availability))
            {
                productsQuery = query.Availability switch
                {
                    "in_stock" => productsQuery.Where(product => product.StockQuantity > 0),
                    "out_of_stock" => productsQuery.Where(product => product.StockQuantity <= 0),
                    "low_stock" => productsQuery.Where(product => product.StockQuantity > 0 && product.StockQuantity <= 5),
                    _ => productsQuery
                };
            }

            productsQuery = query.SortOrder switch
            {
                "price_asc" => productsQuery.OrderBy(product => product.Price),
                "price_desc" => productsQuery.OrderByDescending(product => product.Price),
                "name_asc" => productsQuery.OrderBy(product => product.ProductName),
                "name_desc" => productsQuery.OrderByDescending(product => product.ProductName),
                _ => productsQuery.OrderByDescending(product => product.Id)
            };

            var totalItems = await productsQuery.CountAsync();
            var totalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling((double)totalItems / pageSize);

            if (totalPages > 0 && pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }

            var products = await productsQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.CategoryName)
                .ToListAsync();

            return new StorefrontShopData
            {
                Products = products,
                Categories = categories,
                PageNumber = pageNumber,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<Product?> GetProductAsync(int id)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .FirstOrDefaultAsync(product => product.Id == id);
        }

        public async Task<bool> IsWishlistedAsync(int userId, int productId)
        {
            return await _context.Wishlists
                .AsNoTracking()
                .AnyAsync(wishlist => wishlist.UserId == userId && wishlist.ProductId == productId);
        }

        public async Task<IReadOnlyList<string>> GetSearchSuggestionsAsync(string term)
        {
            var normalizedTerm = term.Trim();

            return await _context.Products
                .AsNoTracking()
                .Where(product =>
                    product.ProductName.Contains(normalizedTerm)
                    || (product.Description != null && product.Description.Contains(normalizedTerm)))
                .OrderBy(product => product.ProductName)
                .Select(product => product.ProductName)
                .Distinct()
                .Take(10)
                .ToListAsync();
        }
    }
}
