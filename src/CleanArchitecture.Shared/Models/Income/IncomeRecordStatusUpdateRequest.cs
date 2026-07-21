using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Income;

public class IncomeRecordStatusUpdateRequest
{
    public IncomeStatus Status { get; set; }
}
