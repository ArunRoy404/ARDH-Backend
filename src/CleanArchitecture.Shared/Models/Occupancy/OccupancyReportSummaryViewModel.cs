namespace CleanArchitecture.Shared.Models.Occupancy;

public class OccupancyReportSummaryViewModel
{
    public int ApartmentCount { get; set; }
    public int TotalReportDays { get; set; }
    public int TotalOccupiedDays { get; set; }
    public int TotalVacantDays { get; set; }

    public decimal TotalExpectedOccupiedRent { get; set; }
    public decimal TotalVacancyRentValue { get; set; }
    public decimal TotalPotentialRent { get; set; }
}
