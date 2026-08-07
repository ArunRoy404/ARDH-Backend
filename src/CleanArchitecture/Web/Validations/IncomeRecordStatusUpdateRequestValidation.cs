using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.Income;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class IncomeRecordStatusUpdateRequestValidation : AbstractValidator<IncomeRecordStatusUpdateRequest>
{
    public IncomeRecordStatusUpdateRequestValidation()
    {
        RuleFor(x => x.Status)
            .NotNull().WithMessage("Status is required. Valid values: Paid, Pending, Overdue, Partial.")
            .IsInEnum().WithMessage("Status is invalid. Valid values: Paid, Pending, Overdue, Partial.");
    }
}
