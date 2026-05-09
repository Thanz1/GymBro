using GymBro.Contracts;
namespace GymBro.Service
{
    public interface IReviewService
    {
        Task<IEnumerable<ReviewDto>> GetReviewsByProductIdAsync(int productId);

        
        Task<bool> AddReviewAsync(ReviewDto reviewDto);
        Task<IEnumerable<ReviewDto>> GetAllReviewsAsync();
        Task<bool> DeleteReviewAsync(int id);
    }
}
