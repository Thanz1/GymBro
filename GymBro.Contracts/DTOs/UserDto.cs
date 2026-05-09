using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymBro.Contracts
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public bool IsActive { get; set; } = true;
        public string? Token { get; set; } // Giữ lại để dùng cho Identity/Login


        public string? Password { get; set; } // Cần khi Create/Edit người dùng
        public string? NewPassword { get; set; }
        public string? Address { get; set; }  // Controller đang gọi thuộc tính này

        // Đã đồng bộ từ NgayTao sang CreatedDate theo logic trước đó
        public DateTime CreatedDate { get; set; }
    }
}
