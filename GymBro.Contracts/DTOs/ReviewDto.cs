using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymBro.Contracts
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; } // Hiển thị tên sản phẩm thay vì đối tượng Product

        public int UserId { get; set; }
        public string? UserName { get; set; }    // Hiển thị tên người dùng thay vì đối tượng User
        
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
