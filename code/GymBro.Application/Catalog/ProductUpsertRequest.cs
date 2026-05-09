namespace GymBro.Application.Catalog
{
    public sealed class ProductUpsertRequest
    {
        public int? Id { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public string? Description { get; init; }
        public decimal Price { get; init; }
        public int StockQuantity { get; init; }
        public int CategoryId { get; init; }
        public string? ExistingImageFileName { get; init; }
        public ProductImageUpload? ImageUpload { get; init; }
    }
}
