using CleanArchitecture.Shared.Models.Equipment;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class EquipmentUpdateRequestValidation : AbstractValidator<EquipmentUpdateRequest>
{
    public EquipmentUpdateRequestValidation()
    {
        RuleFor(x => x.BuildingId)
            .NotEmpty().WithMessage("Building ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Equipment name is required.")
            .MaximumLength(255).WithMessage("Equipment name must not exceed 255 characters.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Equipment type is required.")
            .MaximumLength(100).WithMessage("Equipment type must not exceed 100 characters.");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required.")
            .MaximumLength(255).WithMessage("Brand must not exceed 255 characters.");

        RuleFor(x => x.Model)
            .MaximumLength(255).WithMessage("Model must not exceed 255 characters.");

        RuleFor(x => x.SerialNumber)
            .MaximumLength(255).WithMessage("Serial number must not exceed 255 characters.");

        RuleFor(x => x.InstallDate)
            .NotEmpty().WithMessage("Install date is required.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Equipment status is required.")
            .MaximumLength(50).WithMessage("Equipment status must not exceed 50 characters.");
    }
}
