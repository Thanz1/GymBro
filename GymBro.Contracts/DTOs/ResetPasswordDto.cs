using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymBro.Contracts
{
    public class ResetPasswordDto
    {
        public string Identifier { get; set; } = string.Empty;

        // Mật khẩu mới mà người dùng muốn đặt
        public string NewPassword { get; set; } = string.Empty;

        // (Tùy chọn) Thành có thể thêm thuộc tính xác nhận để kiểm tra ở phía API nếu muốn
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
