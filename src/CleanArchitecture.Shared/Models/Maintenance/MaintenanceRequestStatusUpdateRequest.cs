using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Maintenance;

public class MaintenanceRequestStatusUpdateRequest
{
    public MaintenanceStatus Status { get; set; }
}
