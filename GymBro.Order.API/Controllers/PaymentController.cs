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
    [HttpGet] // Không có tham số {id} ở đây
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAllPayments()
    {
        var payments = await _context.Payments
            // .Include(p => p.Order) // Mở ra nếu cần
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        // Map sang DTO
        var paymentDtos = payments.Select(p => new PaymentDto
        {
            Id = p.Id,
            OrderId = p.OrderId,
            Amount = p.Amount,
            PaymentDate = p.PaymentDate,
            Status = p.Status,
            PaymentMethodName = "Theo đơn hàng", // Hoặc map từ bảng khác
            CustomerName = "Khách hàng" // Tạm thời để string tĩnh
        }).ToList();

        return Ok(paymentDtos);
    }

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

        // Map dữ liệu trong khả năng cho phép của Database Đơn hàng
        var paymentDto = new PaymentDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            Status = payment.Status,

            // Gán thẳng nếu PaymentMethod là dạng chuỗi, hoặc set mặc định
            PaymentMethodName = "Theo đơn hàng",

            // Vì Order DB chỉ chứa UserId, ta sẽ hiển thị ID để giữ chuẩn Microservices
            CustomerName = payment.Order != null ? $"Khách hàng (Mã: {payment.Order.UserId})" : "Khách vãng lai",
            CustomerEmail = "Đang cập nhật...",
            CustomerAddress = "Đang cập nhật..."
        };

        return Ok(paymentDto);
    }
}
