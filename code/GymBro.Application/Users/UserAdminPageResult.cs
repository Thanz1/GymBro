using GymBro.Core;

namespace GymBro.Application.Users
{
    public sealed class UserAdminPageResult
    {
        public IReadOnlyList<User> Items { get; init; } = [];
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalItems { get; init; }
        public int TotalPages { get; init; }
        public string? SearchString { get; init; }
    }
}
