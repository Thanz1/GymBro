using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymBro.Contracts
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }

        // Hiển thị mã đơn hàng hoặc tên khách hàng cho Admin dễ nhìn
        public string? OrderCode { get; set; }

        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }

        // Ví dụ: "Thẻ tín dụng", "Chuyển khoản", "COD"
        public string? PaymentMethodName { get; set; }

        // Ví dụ: "Thành công", "Thất bại", "Đang xử lý"
        public string? Status { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerAddress { get; set; }
    }
}
