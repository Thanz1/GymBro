namespace GymBro.Application.Supply
{
    public interface IInventoryAdminService
    {
        Task<InventoryOverviewResult> GetOverviewAsync(string? searchString);
        Task<InventoryHistoryResult> GetHistoryAsync(int? productId);
        Task<InventoryAdjustData?> GetAdjustDataAsync(int productId, int? newQuantity = null, string? note = null);
        Task<InventoryAdjustCommandResult> AdjustAsync(InventoryAdjustRequest request);
    }
}
