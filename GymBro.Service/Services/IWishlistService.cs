using GymBro.Contracts;
using GymBro.Contracts.DTOs;
namespace GymBro.Service
{
    public interface IWishlistService
    {
        Task<IEnumerable<WishlistDto>> GetWishlistByUserIdAsync(int userId);
        Task<bool> ToggleWishlistAsync(int userId, int productId);
    }
}
