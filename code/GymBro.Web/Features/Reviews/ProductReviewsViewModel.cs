using GymBro.Core;

namespace GymBro.Web.Controllers
{
    public class ProductReviewsViewModel
    {
        public int ProductId { get; set; }
        public string DetailsUrl { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
        public IList<Review> Reviews { get; set; } = new List<Review>();
    }
}
