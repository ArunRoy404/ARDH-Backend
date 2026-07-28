using CleanArchitecture.Shared.Models.Maintenance;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class MaintenanceRequestCreateRequestValidation : AbstractValidator<MaintenanceRequestCreateRequest>
{
    public MaintenanceRequestCreateRequestValidation()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("A valid category is required.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("A valid priority is required.");

        RuleFor(x => x.BuildingId)
            .NotEmpty().WithMessage("Building ID is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid status is required.");

        RuleFor(x => x.EstimatedCost)
            .GreaterThanOrEqualTo(0).WithMessage("Estimated cost must be greater than or equal to 0.");

        RuleFor(x => x.AnnualCost)
            .GreaterThanOrEqualTo(0).WithMessage("Annual cost must be greater than or equal to 0.");
    }
}
