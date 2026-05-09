using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymBro.Core
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        public int Id { get; set; } // <--- Dùng 'Id' cho chuẩn Controller

        [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        // Quan hệ 1-Nhiều với Product
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}