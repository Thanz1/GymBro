using GymBro.Contracts;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Order.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly OrderDbContext _context;

    public PaymentController(OrderDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAll()
    {
        var payments = await _context.Payments
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                OrderId = p.OrderId,
                OrderCode = "#" + p.OrderId,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentMethodName = p.PaymentMethod,
                Status = p.Status
            })
            .ToListAsync();

        return Ok(payments);
    }
}
