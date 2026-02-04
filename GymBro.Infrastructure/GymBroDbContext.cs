using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure
{
    public class GymBroDbContext : DbContext
    {
        public GymBroDbContext(DbContextOptions<GymBroDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
    }
}
