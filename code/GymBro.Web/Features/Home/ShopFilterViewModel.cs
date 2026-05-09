namespace GymBro.Web.Controllers
{
    public class ShopFilterViewModel
    {
        public string? Keyword { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SortOrder { get; set; }
        public string? Availability { get; set; }
    }
}
