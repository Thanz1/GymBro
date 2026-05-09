using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymBro.Core
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string ProductName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string? ImageURL { get; set; }

        // 👇 ĐÂY LÀ CHỖ QUAN TRỌNG: Viết là CategoryId (d thường)
        public int CategoryId { get; set; }

        public int StockQuantity { get; set; }

        [ForeignKey("CategoryId")] // Khớp với tên biến ở trên
        public virtual Category? Category { get; set; }

        // Các quan hệ khác (Giữ nguyên hoặc xóa bớt nếu chưa dùng)
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}