using GymBro.Core;

namespace GymBro.Application.Orders
{
    public sealed class OrderAdminEditData
    {
        public required Order Order { get; init; }
        public IReadOnlyList<string> EditableStatuses { get; init; } = [];
    }
}
