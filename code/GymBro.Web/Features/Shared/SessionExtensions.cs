using System.Text.Json; // <--- Dùng thư viện có sẵn của .NET Core

namespace GymBro.Web.Helpers
{
    public static class SessionExtensions
    {
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            // Thay JsonConvert bằng JsonSerializer
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            // Thay JsonConvert bằng JsonSerializer
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}