using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymBro.Core
{
    [Table("InventoryTransactions")]
    public class InventoryTransaction
    {
        [Key]
        public int Id { get; set; }

        // 👇 ĐÂY LÀ DÒNG QUAN TRỌNG ĐỂ HẾT LỖI
        public int ProductId { get; set; }

        public int QuantityChange { get; set; } // Số lượng thay đổi (+ hoặc -)

        public DateTime CreatedDate { get; set; } = DateTime.Now; // Ngày tạo

        [StringLength(100)]
        public string TransactionType { get; set; } = "Điều chỉnh"; // Loại giao dịch

        [StringLength(500)]
        public string? Note { get; set; } // Ghi chú

        // Khóa ngoại
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        // Nếu muốn liên kết với đơn hàng (tùy chọn)
        public int? OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}