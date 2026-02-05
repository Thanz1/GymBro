using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymBro.Core
{
    [Table("PaymentMethods")]
    public class PaymentMethod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string MethodName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // 👇 BẠN THÊM DÒNG NÀY VÀO ĐỂ HẾT LỖI 👇
        public bool IsActive { get; set; } = true;
    }
}