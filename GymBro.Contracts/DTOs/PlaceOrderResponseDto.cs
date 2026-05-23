namespace GymBro.Contracts;

public class PlaceOrderResponseDto
{
    public int OrderId { get; set; }
    public bool RequiresPayment { get; set; }
    public string Message { get; set; } = string.Empty;
}
