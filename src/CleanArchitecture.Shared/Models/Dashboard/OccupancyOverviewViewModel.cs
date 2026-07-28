namespace CleanArchitecture.Shared.Models.Dashboard;

public class OccupancyOverviewViewModel
{
    public int Occupied { get; set; }
    public int Vacant { get; set; }
    public int Maintenance { get; set; }
    public int Reserved { get; set; }
    public int Total { get; set; }
}
