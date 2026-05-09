using GymBro.Core;

namespace GymBro.Application.Catalog
{
    public sealed class CategoryCommandResult
    {
        public bool Succeeded { get; private init; }
        public bool NotFound { get; private init; }
        public string? Message { get; private init; }
        public Category? Category { get; private init; }
        public IReadOnlyDictionary<string, string[]> Errors { get; private init; } =
            new Dictionary<string, string[]>();

        public static CategoryCommandResult Success(Category category, string message)
        {
            return new CategoryCommandResult
            {
                Succeeded = true,
                Category = category,
                Message = message
            };
        }

        public static CategoryCommandResult Failure(
            IReadOnlyDictionary<string, string[]> errors,
            string? message = null)
        {
            return new CategoryCommandResult
            {
                Message = message,
                Errors = errors
            };
        }

        public static CategoryCommandResult NotFoundResult(string message)
        {
            return new CategoryCommandResult
            {
                NotFound = true,
                Message = message
            };
        }
    }
}
