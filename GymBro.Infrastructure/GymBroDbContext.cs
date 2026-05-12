using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GymBro.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure
{
    public class GymBroDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public GymBroDbContext(DbContextOptions<GymBroDbContext> options) : base(options)
        {
        }

        // --- Danh sách các bảng nghiệp vụ của GymBro ---
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        // LƯU Ý: Không nên khai báo thêm 'public DbSet<User> Users' ở đây 
        // vì IdentityDbContext đã cung cấp sẵn thuộc tính Users kế thừa từ lớp cha.

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // BẮT BUỘC: Phải gọi base.OnModelCreating trước khi cấu hình thêm
            base.OnModelCreating(modelBuilder);

            // SỬA LỖI TRÙNG CỘT: 
            // Đổi từ u.Username thành u.UserName (viết hoa chữ N) để khớp với thuộc tính có sẵn của Identity.
            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserName)
                .IsUnique();

            modelBuilder.Entity<Category>()
                 .Property(c => c.CategoryName).IsRequired();
        }
    }
}