namespace GymBro.Application.Catalog
{
    public sealed class ProductImageUpload
    {
        public string OriginalFileName { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
        public byte[] Content { get; init; } = [];
        public long Length => Content.LongLength;
        public bool HasContent => Content.LongLength > 0;
    }
}
