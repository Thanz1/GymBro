using GymBro.Contracts;
namespace GymBro.Service;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    Task<IEnumerable<ProductDto>> GetNewProductsAsync(int count);
    Task<IEnumerable<ProductDto>> GetBestSellingProductsAsync(int count);
    Task<ProductDto> GetProductByIdAsync(int id);
    Task<bool> CreateProductAsync(CreateProductDto productDto); 
    Task<bool> UpdateProductAsync(int id, ProductDto productDto); 
    Task<bool> DeleteProductAsync(int id); 
    Task<bool> AdjustStockAsync(int id, int newQuantity, string note);
}
