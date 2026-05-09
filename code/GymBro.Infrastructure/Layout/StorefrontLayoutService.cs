using GymBro.Application.Catalog;
using GymBro.Application.Layout;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Layout
{
    public class StorefrontLayoutService : IStorefrontLayoutService
    {
        private readonly GymBroDbContext _context;

        public StorefrontLayoutService(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<StorefrontLayoutData> GetAsync(int? userId)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.CategoryName)
                .Select(category => new CategoryOptionDto
                {
                    Id = category.Id,
                    Name = category.CategoryName
                })
                .ToListAsync();

            var wishlistCount = 0;
            if (userId.HasValue)
            {
                wishlistCount = await _context.Wishlists
                    .AsNoTracking()
                    .CountAsync(item => item.UserId == userId.Value);
            }

            return new StorefrontLayoutData
            {
                Categories = categories,
                WishlistCount = wishlistCount
            };
        }
    }
}
