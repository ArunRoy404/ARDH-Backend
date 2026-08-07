using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.Expenses;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class ExpenseRecordStatusUpdateRequestValidation : AbstractValidator<ExpenseRecordStatusUpdateRequest>
{
    public ExpenseRecordStatusUpdateRequestValidation()
    {
        RuleFor(x => x.Status)
            .NotNull().WithMessage("Status is required. Valid values: Draft, PendingPayment, Paid.")
            .IsInEnum().WithMessage("Status is required. Valid values: Draft, PendingPayment, Paid.");
    }
}
