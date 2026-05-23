using GymBro.Contracts;
using GymBro.Core;
using GymBro.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Order.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    private readonly OrderDbContext _context;

    public OrderController(OrderDbContext context)
    {
        _context = context;
    }

    [HttpPost("place-order")]
    public async Task<ActionResult<PlaceOrderResponseDto>> PlaceOrder(CheckoutDto dto)
    {
        if (dto.CartItems == null || dto.CartItems.Count == 0)
            return BadRequest("Giỏ hàng trống.");

        var paymentMethod = await _context.PaymentMethods
            .FirstOrDefaultAsync(m => m.Id == dto.PaymentMethodId && m.IsActive);

        if (paymentMethod == null)
            return BadRequest("Phương thức thanh toán không hợp lệ.");

        var order = new GymBro.Core.Order
        {
            UserId = dto.UserId,
            OrderDate = dto.OrderDate == default ? DateTime.Now : dto.OrderDate,
            Status = OrderStatus.Pending
        };

        foreach (var item in dto.CartItems)
        {
            order.OrderDetails.Add(new OrderDetail
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Price
            });
        }

        order.TotalAmount = order.OrderDetails.Sum(d => d.Quantity * d.Price);

        var isBankTransfer = IsBankTransfer(paymentMethod.MethodName);
        var payment = new Payment
        {
            Order = order,
            PaymentDate = DateTime.Now,
            Amount = order.TotalAmount,
            PaymentMethod = paymentMethod.MethodName,
            Status = isBankTransfer
                ? PaymentStatus.AwaitingPayment
                : PaymentStatus.CashCollectionPending
        };

        _context.Orders.Add(order);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return Ok(new PlaceOrderResponseDto
        {
            OrderId = order.Id,
            RequiresPayment = isBankTransfer,
            Message = isBankTransfer
                ? "Đơn hàng đã tạo. Vui lòng hoàn tất chuyển khoản."
                : "Đặt hàng thành công. Thanh toán khi nhận hàng."
        });
    }

    [HttpPost("{id}/confirm-payment")]
    public async Task<IActionResult> ConfirmPayment(int id)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.OrderId == id);

        if (payment == null)
            return NotFound("Không tìm thấy thanh toán.");

        if (PaymentStatus.Equals(payment.Status, PaymentStatus.AwaitingPayment))
        {
            payment.Status = PaymentStatus.PendingVerification;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã ghi nhận chuyển khoản. Chờ xác minh." });
        }

        if (PaymentStatus.IsPaid(payment.Status))
            return Ok(new { message = "Đơn hàng đã được thanh toán." });

        return BadRequest("Không thể xác nhận thanh toán ở trạng thái hiện tại.");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return Ok(orders.Select(MapOrder));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetById(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        return Ok(MapOrder(order));
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetByUser(int userId)
    {
        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return Ok(orders.Select(MapOrder));
    }

    [HttpGet("{orderId}/details")]
    public async Task<ActionResult<IEnumerable<OrderDetailDto>>> GetDetails(int orderId)
    {
        var details = await _context.OrderDetails
            .Where(d => d.OrderId == orderId)
            .Select(d => new OrderDetailDto
            {
                Id = d.Id,
                OrderId = d.OrderId,
                ProductId = d.ProductId,
                Quantity = d.Quantity,
                UnitPrice = d.Price
            })
            .ToListAsync();

        return Ok(details);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("{id}/payment-status")]
    public async Task<ActionResult<string>> GetPaymentStatus(int id)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrderId == id);

        return Ok(payment?.Status ?? "Không có");
    }

    private static bool IsBankTransfer(string methodName)
    {
        var name = methodName.ToLowerInvariant();
        return name.Contains("chuyển khoản") || name.Contains("chuyen khoan")
            || name.Contains("bank") || name.Contains("qr");
    }

    private static OrderDto MapOrder(GymBro.Core.Order order) => new()
    {
        Id = order.Id,
        OrderDate = order.OrderDate,
        Status = order.Status,
        TotalAmount = order.TotalAmount,
        UserId = order.UserId ?? 0,
        OrderDetails = order.OrderDetails.Select(d => new OrderDetailDto
        {
            Id = d.Id,
            OrderId = d.OrderId,
            ProductId = d.ProductId,
            Quantity = d.Quantity,
            UnitPrice = d.Price
        }).ToList()
    };
}
