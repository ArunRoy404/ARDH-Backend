using CleanArchitecture.Shared.Models.Equipment;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class EquipmentCreateRequestValidation : AbstractValidator<EquipmentCreateRequest>
{
    public EquipmentCreateRequestValidation()
    {
        RuleFor(x => x.BuildingId)
            .NotEmpty().WithMessage("Building ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Equipment name is required.")
            .MaximumLength(255).WithMessage("Equipment name must not exceed 255 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("A valid equipment type is required.");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required.")
            .MaximumLength(255).WithMessage("Brand must not exceed 255 characters.");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model is required.")
            .MaximumLength(255).WithMessage("Model must not exceed 255 characters.");

        RuleFor(x => x.SerialNumber)
            .NotEmpty().WithMessage("Serial number is required.")
            .MaximumLength(255).WithMessage("Serial number must not exceed 255 characters.");

        RuleFor(x => x.InstallDate)
            .NotEmpty().WithMessage("Install date is required.");

        RuleFor(x => x.WarrantyExpiryDate)
            .NotEmpty().WithMessage("Warranty expiry date is required.");

        RuleFor(x => x.AmcVendorId)
            .NotEmpty().WithMessage("AMC vendor ID is required.");

        RuleFor(x => x.AmcExpiryDate)
            .NotEmpty().WithMessage("AMC expiry date is required.");

        RuleFor(x => x.LastServiceDate)
            .NotEmpty().WithMessage("Last service date is required.");

        RuleFor(x => x.NextServiceDate)
            .NotEmpty().WithMessage("Next service date is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid equipment status is required.");
    }
}
