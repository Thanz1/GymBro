namespace GymBro.Application.Shopping
{
    public interface ICartCheckoutService
    {
        Task<CartMutationResult> AddToCartAsync(
            IReadOnlyCollection<CartItemData> currentCart,
            int productId,
            int quantity);

        Task<CartMutationResult> UpdateCartAsync(
            IReadOnlyCollection<CartItemData> currentCart,
            int productId,
            int quantity);

        CartMutationResult RemoveFromCart(
            IReadOnlyCollection<CartItemData> currentCart,
            int productId);

        Task<CartRefreshResult> RefreshCartAsync(
            IReadOnlyCollection<CartItemData> currentCart,
            bool enforceStock);

        Task<CheckoutPreparationResult> PrepareCheckoutAsync(
            IReadOnlyCollection<CartItemData> currentCart);

        Task<PlaceOrderResult> PlaceOrderAsync(
            int userId,
            IReadOnlyCollection<CartItemData> currentCart,
            int? paymentMethodId);

        Task<PaymentPageResult> GetPaymentPageAsync(int userId, int orderId);

        Task<ConfirmOrderPaymentResult> ConfirmPaymentAsync(
            int userId,
            int orderId,
            string actorName);
    }
}
