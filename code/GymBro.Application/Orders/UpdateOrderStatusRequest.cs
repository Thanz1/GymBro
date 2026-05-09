namespace GymBro.Application.Orders
{
    public sealed class UpdateOrderStatusRequest
    {
        public int Id { get; init; }
        public string Status { get; init; } = string.Empty;
        public string AdminUsername { get; init; } = string.Empty;
    }
}
