using GymBro.Core;

namespace GymBro.Application.Catalog
{
    public sealed class ProductCommandResult
    {
        public bool Succeeded { get; private init; }
        public bool NotFound { get; private init; }
        public string? Message { get; private init; }
        public Product? Product { get; private init; }
        public IReadOnlyDictionary<string, string[]> Errors { get; private init; } =
            new Dictionary<string, string[]>();

        public static ProductCommandResult Success(Product product, string message)
        {
            return new ProductCommandResult
            {
                Succeeded = true,
                Product = product,
                Message = message
            };
        }

        public static ProductCommandResult Failure(
            IReadOnlyDictionary<string, string[]> errors,
            string? message = null)
        {
            return new ProductCommandResult
            {
                Message = message,
                Errors = errors
            };
        }

        public static ProductCommandResult NotFoundResult(string message)
        {
            return new ProductCommandResult
            {
                NotFound = true,
                Message = message
            };
        }
    }
}
