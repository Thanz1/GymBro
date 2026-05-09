using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymBro.Infrastructure
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductDbContext _context;

        // Constructor nạp DbContext để làm việc với Database
        public ProductRepository(ProductDbContext context)
        {
            _context = context;
        }

        // Các logic lấy dữ liệu từ SQL Server sẽ viết ở đây
    }
}