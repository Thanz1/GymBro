using GymBro.Application.Catalog;

namespace GymBro.Application.Files
{
    public interface IProductImageStorage
    {
        Task<string?> SaveAsync(ProductImageUpload? upload);
        Task DeleteIfManagedAsync(string? imageFileName);
    }
}
