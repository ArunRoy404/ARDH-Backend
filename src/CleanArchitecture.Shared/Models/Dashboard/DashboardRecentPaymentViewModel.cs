using System;

namespace CleanArchitecture.Shared.Models.Dashboard;

public class DashboardRecentPaymentViewModel
{
    public string? FlatNumber { get; set; }
    public string? BuildingName { get; set; }
    public string IncomeType { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}
