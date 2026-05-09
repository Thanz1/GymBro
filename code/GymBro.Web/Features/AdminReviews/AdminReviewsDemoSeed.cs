using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Features.AdminReviews
{
    public static class AdminReviewsDemoSeed
    {
        private const string DemoCustomerUsername = "customer.demo";
        private const string WheyProductName = "Whey Isolate Demo";
        private const string ShakerProductName = "Shaker Inox Demo";

        public static async Task SeedAsync(IServiceProvider services, ILogger logger)
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<GymBroDbContext>();

            var customer = await context.Users.FirstOrDefaultAsync(user => user.Username == DemoCustomerUsername);
            if (customer == null)
            {
                logger.LogWarning("AdminReviews demo seed skipped because demo customer was not found.");
                return;
            }

            var whey = await context.Products.FirstOrDefaultAsync(product => product.ProductName == WheyProductName);
            var shaker = await context.Products.FirstOrDefaultAsync(product => product.ProductName == ShakerProductName);

            if (whey == null || shaker == null)
            {
                logger.LogWarning("AdminReviews demo seed skipped because demo products were not found.");
                return;
            }

            var hasDemoReviews = await context.Reviews.AnyAsync(review =>
                review.UserId == customer.Id
                && (review.ProductId == whey.Id || review.ProductId == shaker.Id));

            if (hasDemoReviews)
            {
                logger.LogInformation("AdminReviews demo data is ready.");
                return;
            }

            var now = DateTime.Now;
            context.Reviews.AddRange(
                new Review
                {
                    ProductId = whey.Id,
                    UserId = customer.Id,
                    Rating = 5,
                    Comment = "Vi ngot de uong, tan nhanh va hop de review trong admin.",
                    CreatedDate = now.AddDays(-2)
                },
                new Review
                {
                    ProductId = shaker.Id,
                    UserId = customer.Id,
                    Rating = 4,
                    Comment = "Shaker chac chan, de ve sinh va du de test xoa review.",
                    CreatedDate = now.AddDays(-1).AddHours(-3)
                },
                new Review
                {
                    ProductId = whey.Id,
                    UserId = customer.Id,
                    Rating = 3,
                    Comment = "Ban demo danh gia trung binh de kiem tra bo loc va giao dien.",
                    CreatedDate = now.AddHours(-8)
                });

            await context.SaveChangesAsync();
            logger.LogInformation("Seeded AdminReviews demo data.");
        }
    }
}
