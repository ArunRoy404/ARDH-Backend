using CleanArchitecture.Shared.Models.Income;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class IncomeRecordStatusUpdateRequestValidation : AbstractValidator<IncomeRecordStatusUpdateRequest>
{
    public IncomeRecordStatusUpdateRequestValidation()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid status is required.");
    }
}
