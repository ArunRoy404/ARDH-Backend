using System;

namespace CleanArchitecture.Shared.Models.Occupancy;

public class OccupancyReportItemViewModel
{
    public Guid ApartmentId { get; set; }
    public string FlatNumber { get; set; } = null!;
    public Guid BuildingId { get; set; }
    public string BuildingName { get; set; } = null!;

    public int TotalReportDays { get; set; }
    public int OccupiedDays { get; set; }
    public int VacantDays { get; set; }
    public string OccupiedDurationDisplay { get; set; } = null!;
    public string VacantDurationDisplay { get; set; } = null!;

    public decimal ExpectedOccupiedRent { get; set; }
    public decimal VacancyRentValue { get; set; }
    public decimal TotalPotentialRent { get; set; }
}
