namespace GymBro.Application.Supply
{
    public static class PurchaseOrderStatuses
    {
        public const string Pending = "Chờ duyệt";
        public const string Imported = "Đã nhập kho";

        public static bool IsImported(string? status)
        {
            return string.Equals(status, Imported, StringComparison.OrdinalIgnoreCase);
        }
    }
}
