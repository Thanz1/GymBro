using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymBro.Contracts
{
    public class ServiceResponse<T>
    {
        public bool Success { get; set; } = true;

        // Thông báo phản hồi (Dùng để hiện TempData trên View)
        public string Message { get; set; } = string.Empty;

        // Dữ liệu trả về (Có thể là int Id, DTO hoặc List)[cite: 36, 37]
        public T? Data { get; set; }

        // Danh sách lỗi chi tiết (Hữu ích khi làm Validation)[cite: 39]
        public List<string>? Errors { get; set; }
    }
}
