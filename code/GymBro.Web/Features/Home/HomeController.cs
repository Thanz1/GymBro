using GymBro.Application.Storefront;
using GymBro.Core;
using GymBro.Web.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IStorefrontCatalogService _storefrontCatalogService;

        public HomeController(IStorefrontCatalogService storefrontCatalogService)
        {
            _storefrontCatalogService = storefrontCatalogService;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomeIndexViewModel();

            try
            {
                var storefrontData = await _storefrontCatalogService.GetHomeAsync();
                model.NewProducts = BuildProductCards(storefrontData.NewProducts, "Má»šI", "bi bi-star-fill");
                model.BestSellingProducts = BuildProductCards(storefrontData.BestSellingProducts, "HOT", "bi bi-fire", "bg-warning text-dark");
            }
            catch (Exception ex)
            {
                model.ErrorMessage = "CÃ³ lá»—i xáº£y ra khi táº£i trang chá»§.";
                model.DebugInfo = ex.Message;
            }

            return View(model);
        }

        public async Task<IActionResult> Shop(
            string? keyword,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortOrder,
            string? availability,
            int? page)
        {
            var filters = CreateShopFilters(keyword, categoryId, minPrice, maxPrice, sortOrder, availability);

            try
            {
                var storefrontData = await _storefrontCatalogService.GetShopAsync(new StorefrontShopQuery
                {
                    Keyword = keyword,
                    CategoryId = categoryId,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice,
                    SortOrder = sortOrder,
                    Availability = availability,
                    PageNumber = page.GetValueOrDefault(1)
                });

                var model = new ShopViewModel
                {
                    Categories = storefrontData.Categories.ToList(),
                    Filters = filters,
                    Pagination = new PaginationViewModel
                    {
                        PageNumber = storefrontData.PageNumber,
                        TotalItems = storefrontData.TotalItems,
                        TotalPages = storefrontData.TotalPages
                    },
                    Products = BuildShopProductCards(storefrontData.Products)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                return View(new ShopViewModel
                {
                    Filters = filters,
                    ErrorMessage = "CÃ³ lá»—i xáº£y ra khi táº£i cá»­a hÃ ng.",
                    DebugInfo = ex.Message
                });
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _storefrontCatalogService.GetProductAsync(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            var user = HttpContext.Session.GetObject<User>("User");
            if (user != null)
            {
                ViewBag.IsWishlisted = await _storefrontCatalogService.IsWishlistedAsync(user.Id, id.Value);
            }

            return View(product);
        }

        public async Task<IActionResult> QuickView(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var product = await _storefrontCatalogService.GetProductAsync(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            return PartialView("_QuickView", product);
        }

        [HttpGet]
        public async Task<IActionResult> GetSearchSuggestions(string? term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
            {
                return Json(new { suggestions = Array.Empty<string>() });
            }

            var suggestions = await _storefrontCatalogService.GetSearchSuggestionsAsync(term);
            return Json(new { suggestions });
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        private static List<ProductCardViewModel> BuildProductCards(
            IEnumerable<Product> products,
            string? highlightText = null,
            string? highlightIconCssClass = null,
            string? highlightBadgeCssClass = null)
        {
            return products
                .Select(product => CreateProductCard(product, highlightText, highlightIconCssClass, highlightBadgeCssClass))
                .ToList();
        }

        private static List<ProductCardViewModel> BuildShopProductCards(IEnumerable<Product> products)
        {
            return products
                .Select(product =>
                {
                    var isHot = product.Id % 5 == 0;
                    return CreateProductCard(
                        product,
                        isHot ? "HOT" : null,
                        isHot ? "bi bi-fire" : null,
                        isHot ? "bg-warning text-dark" : null);
                })
                .ToList();
        }

        private static ProductCardViewModel CreateProductCard(
            Product product,
            string? highlightText,
            string? highlightIconCssClass,
            string? highlightBadgeCssClass)
        {
            return new ProductCardViewModel
            {
                Product = product,
                HighlightText = highlightText,
                HighlightIconCssClass = highlightIconCssClass,
                HighlightBadgeCssClass = highlightBadgeCssClass ?? "bg-danger",
                ShowQuickViewAction = true
            };
        }

        private static ShopFilterViewModel CreateShopFilters(
            string? keyword,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortOrder,
            string? availability)
        {
            return new ShopFilterViewModel
            {
                Keyword = keyword,
                CategoryId = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortOrder = sortOrder,
                Availability = availability
            };
        }
    }
}
