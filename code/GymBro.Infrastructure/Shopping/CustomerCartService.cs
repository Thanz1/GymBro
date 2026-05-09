using GymBro.Application.Shopping;
using GymBro.Core;
using Microsoft.EntityFrameworkCore;

namespace GymBro.Infrastructure.Shopping
{
    public class CustomerCartService : ICustomerCartService
    {
        private readonly GymBroDbContext _context;

        public CustomerCartService(GymBroDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<CartItemData>?> GetCartAsync(string username)
        {
            var user = await FindUserAsync(username);
            if (user == null)
            {
                return null;
            }

            return await _context.CartItems
                .AsNoTracking()
                .Include(item => item.Product)
                .Where(item => item.UserId == user.Id)
                .Select(item => new CartItemData
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product!.ProductName,
                    Price = item.Product.Price,
                    Quantity = item.Quantity,
                    ImageURL = item.Product.ImageURL
                })
                .ToListAsync();
        }

        public async Task<CustomerCartCommandResult> AddToCartAsync(
            string username,
            int productId,
            int quantity)
        {
            var user = await FindUserAsync(username);
            if (user == null)
            {
                return CustomerCartCommandResult.UserNotFoundResult("Nguoi dung khong hop le.");
            }

            if (quantity <= 0)
            {
                return CustomerCartCommandResult.Failure("So luong phai lon hon 0.");
            }

            if (quantity > 100)
            {
                return CustomerCartCommandResult.Failure("So luong khong duoc vuot qua 100 san pham.");
            }

            var product = await _context.Products.FirstOrDefaultAsync(item => item.Id == productId);
            if (product == null || product.Price <= 0)
            {
                return CustomerCartCommandResult.ProductNotFoundResult("San pham khong ton tai.");
            }

            if (product.StockQuantity <= 0)
            {
                return CustomerCartCommandResult.Failure("San pham hien da het hang.");
            }

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(item => item.UserId == user.Id && item.ProductId == productId);

            if (existingItem != null)
            {
                var newQuantity = existingItem.Quantity + quantity;
                if (newQuantity > product.StockQuantity)
                {
                    return CustomerCartCommandResult.Failure(
                        $"San pham nay chi con {product.StockQuantity} cai trong kho.");
                }

                existingItem.Quantity = newQuantity;
            }
            else
            {
                if (quantity > product.StockQuantity)
                {
                    return CustomerCartCommandResult.Failure(
                        $"San pham nay chi con {product.StockQuantity} cai trong kho.");
                }

                _context.CartItems.Add(new CartItem
                {
                    UserId = user.Id,
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();
            return CustomerCartCommandResult.Success("Da them vao gio hang!");
        }

        private async Task<User?> FindUserAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            return await _context.Users
                .FirstOrDefaultAsync(user => user.Username == username && user.IsActive);
        }
    }
}
