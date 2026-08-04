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

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required.")
            .MaximumLength(100).WithMessage("State must not exceed 100 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.");

        RuleFor(x => x.TotalFloors)
            .GreaterThan(0).WithMessage("Total floors must be greater than 0.")
            .When(x => x.TotalFloors.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid status is required.")
            .When(x => x.Status.HasValue);
    }
}
