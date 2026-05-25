using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;
namespace GymBro.Infrastructure
{
    public class IdentityDbContext : DbContext
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
        {
        }

        // Chỉ chứa những bảng thuộc về Identity
        public DbSet<User> Users => Set<User>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Bê cấu hình của User từ file cũ sang
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.Password).HasColumnName("Password");
                entity.Property(u => u.Username).HasColumnName("Username");
            });
        }
    }
}
