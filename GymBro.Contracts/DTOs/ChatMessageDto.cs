using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymBro.Contracts
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public string SenderId { get; set; } // Dùng string vì ID của Identity User thường là GUID/String
        public string ReceiverId { get; set; }
        public string SenderName { get; set; } // Hiển thị tên người gửi trên khung chat cho dễ nhìn
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }
}
