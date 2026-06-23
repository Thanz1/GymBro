using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;
namespace GymBro.Infrastructure
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
        {
        }

        // Chỉ chứa những bảng thuộc về Quản lý Sản phẩm / Kho
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
       
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Bê cấu hình của Category từ file cũ sang
            modelBuilder.Entity<Category>()
                .Property(c => c.CategoryName)
                .IsRequired();

            // Product service chỉ sở hữu các bảng catalog/kho. Các navigation sang
            // Order/Identity được bỏ qua để Docker DB mới không kéo nhầm schema service khác.
            modelBuilder.Entity<Product>()
                .Ignore(p => p.OrderDetails);

            modelBuilder.Entity<Review>()
                .Ignore(r => r.User);

            modelBuilder.Entity<InventoryTransaction>()
                .Ignore(i => i.Order);

            modelBuilder.Entity<Supplier>()
                .Ignore(s => s.PurchaseOrders);
        }
    }
}
