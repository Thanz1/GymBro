using System.Text.Json.Serialization;

namespace GymBro.Contracts;

public class UserDto
{
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    public string? Password { get; set; }
    public string? NewPassword { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedDate { get; set; }
}
