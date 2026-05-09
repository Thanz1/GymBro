namespace GymBro.Application.Shopping
{
    public sealed class CartRefreshResult
    {
        public IReadOnlyList<CartItemData> Items { get; init; } = [];
        public bool HasChanges { get; init; }
        public IReadOnlyList<string> Messages { get; init; } = [];
    }
}
