using GymBro.API.DTOs;
using GymBro.Application.Catalog;
using GymBro.Application.Storefront;
using GymBro.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBro.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IStorefrontCatalogService _storefrontCatalogService;
        private readonly IProductAdminService _productAdminService;

        public ProductController(
            IStorefrontCatalogService storefrontCatalogService,
            IProductAdminService productAdminService)
        {
            _storefrontCatalogService = storefrontCatalogService;
            _productAdminService = productAdminService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return Ok(await _storefrontCatalogService.GetProductsAsync());
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Product>> CreateProduct(CreateProductDto request)
        {
            var result = await _productAdminService.CreateAsync(new ProductUpsertRequest
            {
                ProductName = request.ProductName,
                Price = request.Price,
                Description = request.Description,
                CategoryId = request.CategoryId,
                StockQuantity = request.StockQuantity
            });

            if (!result.Succeeded)
            {
                if (result.Errors.Count > 0)
                {
                    var validationErrors = result.Errors.ToDictionary(
                        item => item.Key,
                        item => item.Value);

                    return ValidationProblem(new ValidationProblemDetails(validationErrors)
                    {
                        Detail = result.Message
                    });
                }

                return BadRequest(result.Message ?? "Khong the tao san pham.");
            }

            return Ok(result.Product);
        }
    }
}
