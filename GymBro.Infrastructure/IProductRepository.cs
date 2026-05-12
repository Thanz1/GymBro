using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GymBro.Core;
namespace GymBro.Infrastructure
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();

        // 2. Lấy chi tiết một sản phẩm (Dấu ? nghĩa là có thể trả về null nếu không tìm thấy)
        Task<Product?> GetByIdAsync(int id);

        // 3. Thêm mới sản phẩm
        Task AddAsync(Product product);

        // 4. Cập nhật sản phẩm
        Task UpdateAsync(Product product);

        // 5. Xóa sản phẩm theo ID
        Task DeleteAsync(int id);
    }
}
