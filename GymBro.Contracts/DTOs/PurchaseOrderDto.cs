using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymBro.Contracts
{
    public class PurchaseOrderDto
    {
        public int Id { get; set; } // Thay thế PurchaseOrderID
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = "Mới";
        public decimal TotalAmount { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; } // Dữ liệu phẳng[cite: 47]

        // Danh sách chi tiết phiếu nhập
        public IEnumerable<PurchaseOrderDetailDto> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetailDto>();
    }

    public class PurchaseOrderDetailDto
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; } // Dữ liệu phẳng
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total => Quantity * UnitPrice; // Thành tiền[cite: 46]
    }
}
