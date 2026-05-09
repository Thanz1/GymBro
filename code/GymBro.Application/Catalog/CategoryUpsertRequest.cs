namespace GymBro.Application.Catalog
{
    public sealed class CategoryUpsertRequest
    {
        public int? Id { get; init; }
        public string CategoryName { get; init; } = string.Empty;
    }
}
