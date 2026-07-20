namespace CleanArchitecture.Shared.Models.AmcContract;

public class AmcContractStatsViewModel
{
    public int ActiveCount { get; set; }
    public int ExpiringCount { get; set; }
    public int ExpiredCount { get; set; }
    public int CancelledCount { get; set; }
    public int TotalCount { get; set; }
}
