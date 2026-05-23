using GymBro.Contracts;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    // GET: api/Payment - Lấy toàn bộ danh sách thanh toán
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAllPayments()
    {
        var payments = await _context.Payments
            .Include(p => p.Order)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        var paymentDtos = payments.Select(p => new PaymentDto
        {
            Id = p.Id,
            OrderId = p.OrderId,
            Amount = p.Amount,
            PaymentDate = p.PaymentDate,
            Status = p.Status,
            PaymentMethodName = "Thanh toán khi nhận hàng (COD)",
            CustomerName = p.Order != null ? $"Khách hàng (Mã: {p.Order.UserId})" : "Khách vãng lai",
            CustomerEmail = "Đang cập nhật...",
            CustomerAddress = "Chưa cập nhật địa chỉ"
        }).ToList();

        return Ok(paymentDtos);
    }

    // GET: api/Payment/8 - Lấy chi tiết 1 giao dịch thanh toán theo ID của nó
    [HttpGet("{id}")]
    public async Task<ActionResult<PaymentDto>> GetPaymentById(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            return NotFound();
        }

        var paymentDto = new PaymentDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            Status = payment.Status,
            PaymentMethodName = "Thanh toán khi nhận hàng (COD)",
            CustomerName = payment.Order != null ? $"Khách hàng (Mã: {payment.Order.UserId})" : "Khách vãng lai",
            CustomerEmail = "Đang cập nhật...",
            CustomerAddress = "Chưa cập nhật địa chỉ"
        };

        return Ok(paymentDto);
    }

    // PUT: api/Payment/8/status - API xử lý cập nhật trạng thái xuống Database
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
        {
            return NotFound();
        }

        payment.Status = newStatus;
        await _context.SaveChangesAsync();

        return Ok();
    }
}

