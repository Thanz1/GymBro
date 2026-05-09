using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymBro.Contracts
{
    public class InventoryTransactionDto
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }

        // Tên sản phẩm để hiển thị trực tiếp (Flattening)
        public string? ProductName { get; set; }

        public string TransactionType { get; set; } = string.Empty;
        public int QuantityChange { get; set; }

        // Tham chiếu đến đơn hàng nếu có
        public int? OrderId { get; set; }

        public string? Note { get; set; }
    }
}
