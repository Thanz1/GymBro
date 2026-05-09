using GymBro.Application.Orders;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Orders
{
    public class OrderDetailReadService : IOrderDetailReadService
    {
        private readonly GymBroDbContext _context;

        public OrderDetailReadService(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<OrderDetailListResult> GetListAsync(int? orderId)
        {
            var details = _context.OrderDetails
                .AsNoTracking()
                .Include(item => item.Order)
                .Include(item => item.Product)
                .AsQueryable();

            if (orderId.HasValue)
            {
                details = details.Where(item => item.OrderId == orderId.Value);
            }

            return new OrderDetailListResult
            {
                OrderId = orderId,
                Items = await details
                    .OrderByDescending(item => item.OrderId)
                    .ThenBy(item => item.Id)
                    .ToListAsync()
            };
        }

        public async Task<OrderDetail?> GetByIdAsync(int id)
        {
            return await _context.OrderDetails
                .AsNoTracking()
                .Include(item => item.Order)
                .Include(item => item.Product)
                .FirstOrDefaultAsync(item => item.Id == id);
        }
    }
}
