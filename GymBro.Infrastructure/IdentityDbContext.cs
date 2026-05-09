using GymBro.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace GymBro.Infrastructure;

public sealed class IdentityDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    // LỖI CS0516: Phải gọi base(options) chứ không phải gọi chính nó
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Bắt buộc phải có dòng này

        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserName)
            .IsUnique();
    }
}

