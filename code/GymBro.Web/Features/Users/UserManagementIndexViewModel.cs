using GymBro.Core;

namespace GymBro.Web.Features.Users
{
    public class UserManagementIndexViewModel
    {
        public IReadOnlyList<User> Items { get; init; } = [];
        public string? SearchString { get; init; }
        public int PageNumber { get; init; }
        public int TotalPages { get; init; }
        public int TotalItems { get; init; }
    }
}
