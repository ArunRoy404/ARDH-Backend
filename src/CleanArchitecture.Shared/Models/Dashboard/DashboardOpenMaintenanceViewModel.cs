using System;

namespace CleanArchitecture.Shared.Models.Dashboard;

public class DashboardOpenMaintenanceViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
