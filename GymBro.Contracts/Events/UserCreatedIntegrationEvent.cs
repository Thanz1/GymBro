namespace GymBro.Contracts.Events;

/// <summary>
/// Event bắn lên RabbitMQ khi có user mới (ví dụ: đăng ký lần đầu qua Google).
/// Order.API / email service có thể subscribe để tạo giỏ hàng hoặc gửi email chào mừng.
/// </summary>
public class UserCreatedIntegrationEvent
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string RegistrationSource { get; set; } = string.Empty;
    public bool SendWelcomeEmail { get; set; } = true;
}

public static class IntegrationEventRoutingKeys
{
    public const string UserCreated = "user.created";
}
