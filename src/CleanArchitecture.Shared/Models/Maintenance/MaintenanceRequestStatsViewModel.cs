namespace CleanArchitecture.Shared.Models.Maintenance;

public class MaintenanceRequestStatsViewModel
{
    public int OpenCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompleteCount { get; set; }
    public int CancelledCount { get; set; }
    public int TotalCount { get; set; }
}
