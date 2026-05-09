using GymBro.Application.Auth;
using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Web.Features.AdminPayments
{
    public static class AdminPaymentsDemoSeed
    {
        public const string AdminUsername = "admin.demo";
        public const string AdminPassword = "123456";

        private const string CustomerUsername = "customer.demo";
        private const string CustomerPassword = "123456";
        private const string DemoCategoryName = "Demo AdminPayments";
        private const string WheyProductName = "Whey Isolate Demo";
        private const string ShakerProductName = "Shaker Inox Demo";

        public static async Task SeedAsync(IServiceProvider services, ILogger logger)
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<GymBroDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            await context.Database.MigrateAsync();

            var admin = await EnsureUserAsync(
                context,
                passwordHasher,
                AdminUsername,
                AdminPassword,
                "Admin Demo",
                "admin.demo@gymbro.local",
                "HCM City",
                "Admin");

            var customer = await EnsureUserAsync(
                context,
                passwordHasher,
                CustomerUsername,
                CustomerPassword,
                "Customer Demo",
                "customer.demo@gymbro.local",
                "HCM City",
                "User");

            var category = await EnsureCategoryAsync(context);
            var products = await EnsureProductsAsync(context, category.Id);
            await EnsurePaymentMethodsAsync(context);

            var hasDemoOrders = await context.Orders.AnyAsync(order => order.UserId == customer.Id);
            if (hasDemoOrders)
            {
                logger.LogInformation(
                    "AdminPayments demo data is ready. Admin account: {Username}",
                    admin.Username);
                return;
            }

            var now = DateTime.Now;

            AddDemoOrder(
                context,
                customer,
                "Chờ xác minh",
                PaymentStatus.PendingVerification,
                "Banking",
                now.AddMinutes(-45),
                now.AddMinutes(-20),
                (products.whey, 1),
                (products.shaker, 1));

            AddDemoOrder(
                context,
                customer,
                "Chờ xác minh",
                PaymentStatus.PendingVerification,
                "Banking",
                now.AddHours(-2),
                now.AddMinutes(-75),
                (products.whey, 1));

            AddDemoOrder(
                context,
                customer,
                OrderStatus.Pending,
                PaymentStatus.AwaitingPayment,
                "Banking",
                now.AddHours(-28),
                now.AddHours(-28),
                (products.whey, 2));

            AddDemoOrder(
                context,
                customer,
                OrderStatus.Pending,
                PaymentStatus.AwaitingPayment,
                "Banking",
                now.AddHours(-3),
                now.AddHours(-3),
                (products.shaker, 2));

            AddDemoOrder(
                context,
                customer,
                "Đang xử lý",
                PaymentStatus.CashCollectionPending,
                "COD",
                now.AddHours(-6),
                now.AddHours(-6),
                (products.whey, 1),
                (products.shaker, 2));

            await context.SaveChangesAsync();

            logger.LogInformation(
                "Seeded AdminPayments demo data. Admin login: {Username} / {Password}",
                AdminUsername,
                AdminPassword);
        }

        private static async Task<User> EnsureUserAsync(
            GymBroDbContext context,
            IPasswordHasher passwordHasher,
            string username,
            string plainPassword,
            string fullName,
            string email,
            string address,
            string role)
        {
            var user = await context.Users.FirstOrDefaultAsync(item => item.Username == username);
            if (user == null)
            {
                user = new User
                {
                    Username = username,
                    CreatedDate = DateTime.Now
                };
                context.Users.Add(user);
            }

            user.FullName = fullName;
            user.Email = email;
            user.Address = address;
            user.Role = role;
            user.IsActive = true;

            if (string.IsNullOrWhiteSpace(user.Password) || !passwordHasher.VerifyPassword(plainPassword, user.Password))
            {
                user.Password = passwordHasher.HashPassword(plainPassword);
            }

            await context.SaveChangesAsync();
            return user;
        }

        private static async Task<Category> EnsureCategoryAsync(GymBroDbContext context)
        {
            var category = await context.Categories.FirstOrDefaultAsync(item => item.CategoryName == DemoCategoryName);
            if (category != null)
            {
                return category;
            }

            category = new Category
            {
                CategoryName = DemoCategoryName
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();
            return category;
        }

        private static async Task<(Product whey, Product shaker)> EnsureProductsAsync(GymBroDbContext context, int categoryId)
        {
            var whey = await EnsureProductAsync(
                context,
                WheyProductName,
                categoryId,
                1290000m,
                30,
                "Whey demo cho flow AdminPayments.");

            var shaker = await EnsureProductAsync(
                context,
                ShakerProductName,
                categoryId,
                180000m,
                50,
                "Shaker demo cho flow AdminPayments.");

            return (whey, shaker);
        }

        private static async Task<Product> EnsureProductAsync(
            GymBroDbContext context,
            string productName,
            int categoryId,
            decimal price,
            int stockQuantity,
            string description)
        {
            var product = await context.Products.FirstOrDefaultAsync(item => item.ProductName == productName);
            if (product == null)
            {
                product = new Product
                {
                    ProductName = productName
                };
                context.Products.Add(product);
            }

            product.CategoryId = categoryId;
            product.Price = price;
            product.StockQuantity = stockQuantity;
            product.Description = description;
            product.ImageURL = "/Content/Images/shopping.webp";

            await context.SaveChangesAsync();
            return product;
        }

        private static async Task EnsurePaymentMethodsAsync(GymBroDbContext context)
        {
            await EnsurePaymentMethodAsync(
                context,
                "Thanh toán khi nhận hàng (COD)",
                "Dữ liệu mẫu cho local test.");

            await EnsurePaymentMethodAsync(
                context,
                "Chuyển khoản Ngân hàng (QR Code)",
                "Dữ liệu mẫu cho local test.");
        }

        private static async Task EnsurePaymentMethodAsync(
            GymBroDbContext context,
            string methodName,
            string description)
        {
            var method = await context.PaymentMethods.FirstOrDefaultAsync(item => item.MethodName == methodName);
            if (method == null)
            {
                method = new PaymentMethod
                {
                    MethodName = methodName
                };
                context.PaymentMethods.Add(method);
            }

            method.Description = description;
            method.IsActive = true;

            await context.SaveChangesAsync();
        }

        private static void AddDemoOrder(
            GymBroDbContext context,
            User customer,
            string orderStatus,
            string paymentStatus,
            string paymentMethod,
            DateTime orderDate,
            DateTime paymentDate,
            params (Product product, int quantity)[] items)
        {
            var totalAmount = items.Sum(item => item.product.Price * item.quantity);
            var order = new Order
            {
                UserId = customer.Id,
                OrderDate = orderDate,
                Status = orderStatus,
                TotalAmount = totalAmount
            };

            context.Orders.Add(order);

            foreach (var item in items)
            {
                context.OrderDetails.Add(new OrderDetail
                {
                    Order = order,
                    ProductId = item.product.Id,
                    Quantity = item.quantity,
                    Price = item.product.Price
                });
            }

            context.Payments.Add(new Payment
            {
                Order = order,
                PaymentDate = paymentDate,
                Amount = totalAmount,
                PaymentMethod = paymentMethod,
                Status = paymentStatus
            });
        }
    }
}
