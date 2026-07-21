using CleanArchitecture.Shared.Models.Expenses;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class ExpenseRecordStatusUpdateRequestValidation : AbstractValidator<ExpenseRecordStatusUpdateRequest>
{
    public ExpenseRecordStatusUpdateRequestValidation()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid status is required.");
    }
}
