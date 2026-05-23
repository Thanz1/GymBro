namespace GymBro.Infrastructure.Auth;

/// <summary>
/// Hỗ trợ mật khẩu BCrypt (mới) và plain text (dữ liệu migrate cũ).
/// </summary>
public static class PasswordHelper
{
    public static bool Verify(string plainPassword, string storedPassword, out bool needsRehash)
    {
        needsRehash = false;
        if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(storedPassword))
            return false;

        var stored = storedPassword.Trim();

        // BCrypt hash thường bắt đầu bằng $2a$, $2b$, $2y$
        if (stored.StartsWith("$2", StringComparison.Ordinal))
            return BCrypt.Net.BCrypt.Verify(plainPassword, stored);

        // Dữ liệu migrate cũ: lưu plain text trong cột Password
        if (string.Equals(plainPassword, stored, StringComparison.Ordinal))
        {
            needsRehash = true;
            return true;
        }

        return false;
    }

    public static string Hash(string plainPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainPassword);
}
