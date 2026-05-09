using GymBro.Application.Layout;
using GymBro.Application.Payments;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Layout
{
    public class AdminLayoutService : IAdminLayoutService
    {
        private readonly GymBroDbContext _context;

        public AdminLayoutService(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<AdminLayoutData> GetAsync()
        {
            return new AdminLayoutData
            {
                PendingPaymentCount = await _context.Payments
                    .AsNoTracking()
                    .CountAsync(payment => payment.Status == PaymentStatus.PendingVerification)
            };
        }
    }
}
