namespace GymBro.Application.Shopping
{
    public sealed class CartMutationResult
    {
        public bool Succeeded { get; private init; }
        public bool IsWarning { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public IReadOnlyList<CartItemData> Items { get; private init; } = [];

        public static CartMutationResult Success(
            IReadOnlyList<CartItemData> items,
            string message)
        {
            return new CartMutationResult
            {
                Succeeded = true,
                Message = message,
                Items = items
            };
        }

        public static CartMutationResult Warning(
            IReadOnlyList<CartItemData> items,
            string message)
        {
            return new CartMutationResult
            {
                IsWarning = true,
                Message = message,
                Items = items
            };
        }

        public static CartMutationResult Failure(
            IReadOnlyList<CartItemData> items,
            string message)
        {
            return new CartMutationResult
            {
                Message = message,
                Items = items
            };
        }
    }
}
