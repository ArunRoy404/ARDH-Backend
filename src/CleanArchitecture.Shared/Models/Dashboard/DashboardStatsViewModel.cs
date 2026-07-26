using System;

namespace CleanArchitecture.Shared.Models.Dashboard;

public class DashboardStatsViewModel
{
    public int TotalBuildings { get; set; }
    public int TotalApartments { get; set; }
    public int OccupiedCount { get; set; }
    public int VacantCount { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpense { get; set; }
    public int PendingPaymentsCount { get; set; }
    public int OpenMaintenanceCount { get; set; }
}
