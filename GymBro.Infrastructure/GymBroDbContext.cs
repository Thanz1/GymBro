using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure;

public class GymBroDbContext : DbContext
{
    public GymBroDbContext(DbContextOptions<GymBroDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderDetail> PurchaseOrderDetails => Set<PurchaseOrderDetail>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Password).HasColumnName("Password");
            entity.Property(u => u.Username).HasColumnName("Username");
        });

        modelBuilder.Entity<Category>()
            .Property(c => c.CategoryName)
            .IsRequired();
    }
}
