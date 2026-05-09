using GymBro.Core;

namespace GymBro.Application.Supply
{
    public sealed class SupplierCommandResult
    {
        public bool Succeeded { get; private init; }
        public bool NotFound { get; private init; }
        public string? Message { get; private init; }
        public Supplier? Supplier { get; private init; }
        public IReadOnlyDictionary<string, string[]> Errors { get; private init; } =
            new Dictionary<string, string[]>();

        public static SupplierCommandResult Success(Supplier supplier, string message)
        {
            return new SupplierCommandResult
            {
                Succeeded = true,
                Supplier = supplier,
                Message = message
            };
        }

        public static SupplierCommandResult Failure(
            IReadOnlyDictionary<string, string[]> errors,
            string? message = null)
        {
            return new SupplierCommandResult
            {
                Message = message,
                Errors = errors
            };
        }

        public static SupplierCommandResult NotFoundResult(string message)
        {
            return new SupplierCommandResult
            {
                NotFound = true,
                Message = message
            };
        }
    }
}
