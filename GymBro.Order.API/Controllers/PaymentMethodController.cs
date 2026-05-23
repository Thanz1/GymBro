using GymBro.Contracts;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Order.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentMethodController : ControllerBase
{
    private readonly OrderDbContext _context;

    public PaymentMethodController(OrderDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentMethodDto>>> GetAll()
    {
        var methods = await _context.PaymentMethods
            .Where(m => m.IsActive)
            .Select(m => new PaymentMethodDto
            {
                Id = m.Id,
                MethodName = m.MethodName,
                Description = m.Description,
                IsActive = m.IsActive
            })
            .ToListAsync();

        return Ok(methods);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PaymentMethodDto>> GetById(int id)
    {
        var method = await _context.PaymentMethods.FindAsync(id);
        if (method == null) return NotFound();

        return Ok(new PaymentMethodDto
        {
            Id = method.Id,
            MethodName = method.MethodName,
            Description = method.Description,
            IsActive = method.IsActive
        });
    }
}
