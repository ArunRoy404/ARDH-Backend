using CleanArchitecture.Shared.Models.Building;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class BuildingCreateRequestValidation : AbstractValidator<BuildingCreateRequest>
{
    public BuildingCreateRequestValidation()
    {
        RuleFor(x => x.BuildingName)
            .NotEmpty().WithMessage("Building name is required.")
            .MaximumLength(255).WithMessage("Building name must not exceed 255 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required.")
            .MaximumLength(100).WithMessage("State must not exceed 100 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.");

        RuleFor(x => x.GoogleMapLink)
            .NotEmpty().WithMessage("Google Map link is required.");

        RuleFor(x => x.TotalFloors)
            .GreaterThan(0).WithMessage("Total floors must be greater than 0.");

        RuleFor(x => x.ParkingDetails)
            .NotEmpty().WithMessage("Parking details are required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid status is required.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        RuleFor(x => x.ImageUrl)
            .NotEmpty().WithMessage("Image URL is required.");
    }
}
