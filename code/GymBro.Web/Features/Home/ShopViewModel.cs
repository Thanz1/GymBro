using GymBro.Core;

namespace GymBro.Web.Controllers
{
    public class ShopViewModel
    {
        public IList<ProductCardViewModel> Products { get; set; } = new List<ProductCardViewModel>();
        public IList<Category> Categories { get; set; } = new List<Category>();
        public ShopFilterViewModel Filters { get; set; } = new ShopFilterViewModel();
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();
        public string? ErrorMessage { get; set; }
        public string? DebugInfo { get; set; }
    }
}
