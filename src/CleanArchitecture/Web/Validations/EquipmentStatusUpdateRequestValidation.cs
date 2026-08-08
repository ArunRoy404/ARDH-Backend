using CleanArchitecture.Shared.Models.Equipment;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class EquipmentStatusUpdateRequestValidation : AbstractValidator<EquipmentStatusUpdateRequest>
{
    public EquipmentStatusUpdateRequestValidation()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Equipment status is required.")
            .MaximumLength(50).WithMessage("Equipment status must not exceed 50 characters.");
    }
}
