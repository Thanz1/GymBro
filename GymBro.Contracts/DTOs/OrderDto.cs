using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymBro.Contracts
{
    public class OrderDto
    {
        public int Id { get; set; } // Thay thế cho OrderID[cite: 36, 37, 39]
        public DateTime OrderDate { get; set; }
  
        public string Status { get; set; } = "Chờ xử lý"; 
        public decimal TotalAmount { get; set; }
      

        // 2. Thông tin khách hàng (Đã phẳng hóa để View hiển thị luôn)
        public int UserId { get; set; }
     
        public string? CustomerName { get; set; } // Lấy từ User.FullName[cite: 37, 38, 39]
        public string? CustomerEmail { get; set; } // Lấy từ User.Email
        public string? CustomerAddress { get; set; } // Lấy từ User.Address

        // 3. Danh sách chi tiết các sản phẩm trong đơn hàng
        // Giúp trang Details hiển thị bảng hóa đơn[cite: 37]
        public IEnumerable<OrderDetailDto> OrderDetails { get; set; } = new List<OrderDetailDto>();
    }
}
