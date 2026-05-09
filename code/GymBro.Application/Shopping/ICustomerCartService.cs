namespace GymBro.Application.Shopping
{
    public interface ICustomerCartService
    {
        Task<IReadOnlyList<CartItemData>?> GetCartAsync(string username);

        Task<CustomerCartCommandResult> AddToCartAsync(
            string username,
            int productId,
            int quantity);
    }
}
