namespace GymBro.Web.Features.Orders
{
    public class OrderManagementEditViewModel
    {
        public int Id { get; init; }
        public string CustomerDisplayName { get; init; } = string.Empty;
        public int? UserId { get; init; }
        public DateTime OrderDate { get; init; }
        public decimal TotalAmount { get; init; }
        public string Status { get; set; } = string.Empty;
        public IReadOnlyList<string> StatusOptions { get; init; } = [];
    }
}
