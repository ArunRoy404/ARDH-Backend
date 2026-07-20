using CleanArchitecture.Shared.Models.Apartment;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class ApartmentCreateRequestValidation : AbstractValidator<ApartmentCreateRequest>
{
    public ApartmentCreateRequestValidation()
    {
        RuleFor(x => x.BuildingId)
            .NotEmpty().WithMessage("Building ID is required.");

        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("Owner ID is required.");

        RuleFor(x => x.NestawayId)
            .NotEmpty().WithMessage("Nestaway ID is required.")
            .MaximumLength(50).WithMessage("Nestaway ID cannot exceed 50 characters.");

        RuleFor(x => x.FlatNumber)
            .NotEmpty().WithMessage("Flat number is required.")
            .MaximumLength(20).WithMessage("Flat number cannot exceed 20 characters.");

        RuleFor(x => x.Floor)
            .GreaterThanOrEqualTo(0).WithMessage("Floor number cannot be negative.");

        RuleFor(x => x.ApartmentType)
            .IsInEnum().WithMessage("Invalid apartment type.");

        RuleFor(x => x.AreaSqft)
            .GreaterThan(0).WithMessage("Area sqft must be greater than 0.");

        RuleFor(x => x.Bedrooms)
            .GreaterThanOrEqualTo(0).WithMessage("Bedrooms cannot be negative.");

        RuleFor(x => x.Bathrooms)
            .GreaterThanOrEqualTo(0).WithMessage("Bathrooms cannot be negative.");

        RuleFor(x => x.ParkingSlot)
            .NotEmpty().WithMessage("Parking slot is required.")
            .MaximumLength(50).WithMessage("Parking slot cannot exceed 50 characters.");

        RuleFor(x => x.ExpectedRent)
            .GreaterThanOrEqualTo(0).WithMessage("Expected rent cannot be negative.");

        RuleFor(x => x.MaintenanceCharge)
            .GreaterThanOrEqualTo(0).WithMessage("Maintenance charge cannot be negative.");

        RuleFor(x => x.WaterCharge)
            .GreaterThanOrEqualTo(0).WithMessage("Water charge cannot be negative.");
    }
}
