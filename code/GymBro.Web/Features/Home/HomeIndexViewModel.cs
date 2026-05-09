namespace GymBro.Web.Controllers
{
    public class HomeIndexViewModel
    {
        public IList<ProductCardViewModel> NewProducts { get; set; } = new List<ProductCardViewModel>();
        public IList<ProductCardViewModel> BestSellingProducts { get; set; } = new List<ProductCardViewModel>();
        public string? ErrorMessage { get; set; }
        public string? DebugInfo { get; set; }
    }
}
