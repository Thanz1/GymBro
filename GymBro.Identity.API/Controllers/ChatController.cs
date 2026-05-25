using GymBro.Infrastructure;
using GymBro.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace GymBro.Identity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IdentityDbContext _context;

        public ChatController(IdentityDbContext context)
        {
            _context = context;
        }

        // API 1: Lấy toàn bộ lịch sử trò chuyện giữa Khách và Admin
        [HttpGet("history")]
        public async Task<IActionResult> GetChatHistory([FromQuery] string user1, [FromQuery] string user2)
        {
            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == user1 && m.ReceiverId == user2) ||
                            (m.SenderId == user2 && m.ReceiverId == user1))
                .OrderBy(m => m.Timestamp)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    Content = m.Content,
                    Timestamp = m.Timestamp,
                    IsRead = m.IsRead
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpGet("admin/conversations")]
        public async Task<IActionResult> GetConversationsForAdmin([FromQuery] string adminId)
        {
            try
            {
                // 1. Kéo dữ liệu thô về (tránh lỗi EF Core không dịch được IF/ELSE)
                var messages = await _context.ChatMessages
                    .Where(m => m.SenderId == adminId || m.ReceiverId == adminId)
                    .Select(m => new { m.SenderId, m.ReceiverId })
                    .ToListAsync();

                // 2. Lọc danh sách ID trên RAM
                var userIds = messages
                    .Select(m => m.SenderId == adminId ? m.ReceiverId : m.SenderId)
                    .Distinct()
                    .ToList();

                // 3. Ép kiểu an toàn (chỉ lấy những ID là số)
                var validIntIds = userIds
                    .Where(id => int.TryParse(id, out _))
                    .Select(int.Parse)
                    .ToList();

                // 4. Lấy thông tin User từ Database
                var conversations = await _context.Users
                    .Where(u => validIntIds.Contains(u.Id))
                    .Select(u => new
                    {
                        UserId = u.Id.ToString(),
                        FullName = u.FullName,
                        Username = u.Username
                    })
                    .ToListAsync();

                // 5. Thêm các "Khách vãng lai" vào danh sách
                var guestIds = userIds.Where(id => id.StartsWith("Guest_")).ToList();
                foreach (var guestId in guestIds)
                {
                    conversations.Add(new { UserId = guestId, FullName = "Khách vãng lai", Username = guestId });
                }

                return Ok(conversations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }
    }
}
