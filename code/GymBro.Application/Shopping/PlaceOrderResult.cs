namespace GymBro.Application.Shopping
{
    public sealed class PlaceOrderResult
    {
        public bool Succeeded { get; private init; }
        public bool RequiresCheckoutReview { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public int? OrderId { get; private init; }
        public bool RedirectToPayment { get; private init; }
        public CheckoutPreparationResult? Checkout { get; private init; }

        public static PlaceOrderResult Success(
            int orderId,
            bool redirectToPayment,
            string message)
        {
            return new PlaceOrderResult
            {
                Succeeded = true,
                OrderId = orderId,
                RedirectToPayment = redirectToPayment,
                Message = message
            };
        }

        public static PlaceOrderResult CheckoutReview(
            CheckoutPreparationResult checkout,
            string message)
        {
            return new PlaceOrderResult
            {
                RequiresCheckoutReview = true,
                Checkout = checkout,
                Message = message
            };
        }

        public static PlaceOrderResult Failure(string message)
        {
            return new PlaceOrderResult
            {
                Message = message
            };
        }
    }
}
