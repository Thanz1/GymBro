namespace GymBro.Application.Users
{
    public sealed class UserAdminPageQuery
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SearchString { get; init; }
    }
}
