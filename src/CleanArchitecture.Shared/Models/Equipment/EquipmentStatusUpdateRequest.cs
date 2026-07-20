using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Equipment;

public class EquipmentStatusUpdateRequest
{
    public EquipmentStatus Status { get; set; }
}
