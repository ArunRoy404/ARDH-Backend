using CleanArchitecture.Shared.Models.Equipment;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class EquipmentStatusUpdateRequestValidation : AbstractValidator<EquipmentStatusUpdateRequest>
{
    public EquipmentStatusUpdateRequestValidation()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid equipment status is required.");
    }
}
