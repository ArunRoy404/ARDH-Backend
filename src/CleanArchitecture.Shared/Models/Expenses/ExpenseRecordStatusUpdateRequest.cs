using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.Expenses;

public class ExpenseRecordStatusUpdateRequest
{
    public ExpenseStatus Status { get; set; }
}
