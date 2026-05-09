using GymBro.Application.Catalog;
using GymBro.Application.Shared;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Catalog
{
    public class CategoryAdminService : ICategoryAdminService
    {
        private readonly GymBroDbContext _context;

        public CategoryAdminService(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Category>> GetListAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .OrderBy(category => category.CategoryName)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(category => category.Id == id);
        }

        public async Task<CategoryCommandResult> CreateAsync(CategoryUpsertRequest request)
        {
            var errors = await ValidateAsync(request);
            if (errors.Count > 0)
            {
                return CategoryCommandResult.Failure(errors);
            }

            var category = new Category
            {
                CategoryName = request.CategoryName.Trim()
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CategoryCommandResult.Success(category, "Đã tạo danh mục thành công.");
        }

        public async Task<CategoryCommandResult> UpdateAsync(CategoryUpsertRequest request)
        {
            if (!request.Id.HasValue)
            {
                return CategoryCommandResult.NotFoundResult("Không tìm thấy danh mục cần cập nhật.");
            }

            var category = await _context.Categories.FindAsync(request.Id.Value);
            if (category == null)
            {
                return CategoryCommandResult.NotFoundResult("Không tìm thấy danh mục cần cập nhật.");
            }

            var errors = await ValidateAsync(request);
            if (errors.Count > 0)
            {
                return CategoryCommandResult.Failure(errors);
            }

            category.CategoryName = request.CategoryName.Trim();
            await _context.SaveChangesAsync();

            return CategoryCommandResult.Success(category, "Đã cập nhật danh mục thành công.");
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return OperationResult.NotFoundResult("Không tìm thấy danh mục cần xóa.");
            }

            var hasProducts = await _context.Products.AnyAsync(product => product.CategoryId == id);
            if (hasProducts)
            {
                return OperationResult.Failure("Không thể xóa danh mục này vì còn sản phẩm thuộc danh mục. Vui lòng chuyển hoặc xóa sản phẩm trước.");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return OperationResult.Success("Đã xóa danh mục thành công.");
        }

        private async Task<Dictionary<string, string[]>> ValidateAsync(CategoryUpsertRequest request)
        {
            var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(request.CategoryName))
            {
                AddError(errors, nameof(request.CategoryName), "Tên danh mục không được để trống.");
                return ToReadOnly(errors);
            }

            var normalizedName = request.CategoryName.Trim();

            var duplicateExists = await _context.Categories.AnyAsync(category =>
                category.Id != (request.Id ?? 0)
                && category.CategoryName.ToLower() == normalizedName.ToLower());

            if (duplicateExists)
            {
                AddError(errors, nameof(request.CategoryName), "Tên danh mục đã tồn tại.");
            }

            return ToReadOnly(errors);
        }

        private static void AddError(
            IDictionary<string, List<string>> errors,
            string key,
            string message)
        {
            if (!errors.TryGetValue(key, out var values))
            {
                values = [];
                errors[key] = values;
            }

            values.Add(message);
        }

        private static Dictionary<string, string[]> ToReadOnly(
            IDictionary<string, List<string>> errors)
        {
            return errors.ToDictionary(
                item => item.Key,
                item => item.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
