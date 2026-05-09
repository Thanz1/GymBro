using GymBro.Application.Account;
using GymBro.Application.Auth;
using GymBro.Application.Catalog;
using GymBro.Application.Files;
using GymBro.Application.Layout;
using GymBro.Application.Orders;
using GymBro.Application.Payments;
using GymBro.Application.Shopping;
using GymBro.Application.Storefront;
using GymBro.Application.Supply;
using GymBro.Application.Users;
using GymBro.Infrastructure.Account;
using GymBro.Infrastructure.Auth;
using GymBro.Infrastructure.Catalog;
using GymBro.Infrastructure.Files;
using GymBro.Infrastructure.Layout;
using GymBro.Infrastructure.Orders;
using GymBro.Infrastructure.Payments;
using GymBro.Infrastructure.Shopping;
using GymBro.Infrastructure.Storefront;
using GymBro.Infrastructure.Supply;
using GymBro.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymBro.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<GymBroDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IProductImageStorage, LocalProductImageStorage>();
            services.AddScoped<ICategoryAdminService, CategoryAdminService>();
            services.AddScoped<IProductAdminService, ProductAdminService>();
            services.AddScoped<IStorefrontLayoutService, StorefrontLayoutService>();
            services.AddScoped<IAdminLayoutService, AdminLayoutService>();
            services.AddScoped<IStorefrontCatalogService, StorefrontCatalogService>();
            services.AddScoped<IOrderInventoryService, OrderInventoryService>();
            services.AddScoped<IOrderAdminService, OrderAdminService>();
            services.AddScoped<IOrderDetailReadService, OrderDetailReadService>();
            services.AddScoped<IPaymentAuditService, PaymentAuditService>();
            services.AddScoped<IPaymentMethodAdminService, PaymentMethodAdminService>();
            services.AddScoped<IPaymentWorkflowService, PaymentWorkflowService>();
            services.AddScoped<ISupplierAdminService, SupplierAdminService>();
            services.AddScoped<IPurchaseOrderAdminService, PurchaseOrderAdminService>();
            services.AddScoped<IInventoryAdminService, InventoryAdminService>();
            services.AddScoped<ICartCheckoutService, CartCheckoutService>();
            services.AddScoped<ICustomerCartService, CustomerCartService>();

            return services;
        }
    }
}
