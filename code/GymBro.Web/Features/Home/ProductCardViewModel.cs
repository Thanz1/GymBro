using GymBro.Core;

namespace GymBro.Web.Controllers
{
    public class ProductCardViewModel
    {
        public Product Product { get; set; } = new();
        public string? HighlightText { get; set; }
        public string? HighlightIconCssClass { get; set; }
        public string HighlightBadgeCssClass { get; set; } = "bg-danger";
        public bool ShowQuickViewAction { get; set; } = true;
    }
}
