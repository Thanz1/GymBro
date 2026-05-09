using GymBro.Application.Catalog;
using GymBro.Application.Files;
using Microsoft.AspNetCore.Hosting;

namespace GymBro.Infrastructure.Files
{
    public class LocalProductImageStorage : IProductImageStorage
    {
        private readonly IWebHostEnvironment _environment;

        public LocalProductImageStorage(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string?> SaveAsync(ProductImageUpload? upload)
        {
            if (upload == null || !upload.HasContent)
            {
                return null;
            }

            var uploadsFolder = GetUploadsFolder();
            Directory.CreateDirectory(uploadsFolder);

            var sanitizedFileName = Path.GetFileName(upload.OriginalFileName);
            var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            await File.WriteAllBytesAsync(filePath, upload.Content);
            return uniqueFileName;
        }

        public Task DeleteIfManagedAsync(string? imageFileName)
        {
            if (string.IsNullOrWhiteSpace(imageFileName))
            {
                return Task.CompletedTask;
            }

            var fileName = Path.GetFileName(imageFileName);
            if (string.Equals(fileName, "shopping.webp", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            var filePath = Path.Combine(GetUploadsFolder(), fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return Task.CompletedTask;
        }

        private string GetUploadsFolder()
        {
            var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;

            return Path.Combine(webRootPath, "Content", "Images");
        }
    }
}
