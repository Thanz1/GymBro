using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymBro.Core
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now; // <--- Tên biến chuẩn

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // Số tiền

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "COD"; // Phương thức (COD/Banking)

        [StringLength(50)]
        public string Status { get; set; } = "Completed"; // Trạng thái

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}