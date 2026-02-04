using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymBro.Core
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        public string TenSanPham { get; set; } = string.Empty; // Ví dụ: "Tạ tay 5kg", "Thảm Yoga"
        [Column(TypeName = "decimal(18,2)")] // Định dạng tiền tệ chuẩn SQL
        public decimal Gia { get; set; }
        public string MoTa { get; set; } = string.Empty;
        public string HinhAnhUrl { get; set; } = string.Empty;
        public string DanhMuc { get; set; } = string.Empty; // Ví dụ: "Dụng cụ", "Quần áo"
        public int SoLuongTon { get; set; }
    }
}
