using Microsoft.AspNetCore.SignalR;
using GymBro.Core;
using GymBro.Contracts;
using GymBro.Infrastructure;

namespace GymBro.Identity.API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IdentityDbContext _context;

        public ChatHub(IdentityDbContext context)
        {
            _context = context;
        }

        // 1. Khi một người kết nối vào Hub, tự động nhét họ vào Nhóm riêng mang tên ID của họ
        public async Task JoinChat(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        // 2. Hàm gửi tin nhắn chuẩn chỉnh
        public async Task SendMessage(ChatMessageDto messageDto)
        {
            // Lưu vào Database
            var chatMessage = new ChatMessage
            {
                SenderId = messageDto.SenderId,
                ReceiverId = messageDto.ReceiverId,
                Content = messageDto.Content,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Gán lại ID vừa sinh tự động dưới DB cho DTO để Frontend quản lý
            messageDto.Id = chatMessage.Id;
            messageDto.Timestamp = chatMessage.Timestamp;

            // Bắn tin nhắn đến Nhóm của Người Nhận (để họ thấy realtime)
            await Clients.Group(messageDto.ReceiverId).SendAsync("ReceiveMessage", messageDto);

            // Bắn ngược lại Nhóm của Người Gửi (để nếu họ mở nhiều tab trình duyệt thì các tab tự đồng bộ)
            await Clients.Group(messageDto.SenderId).SendAsync("ReceiveMessage", messageDto);
        }
    }
}
