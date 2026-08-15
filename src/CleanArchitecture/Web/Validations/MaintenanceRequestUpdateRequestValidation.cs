using CleanArchitecture.Shared.Models.Maintenance;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class MaintenanceRequestUpdateRequestValidation : AbstractValidator<MaintenanceRequestUpdateRequest>
{
    public MaintenanceRequestUpdateRequestValidation()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters.");

        RuleFor(x => x.Priority)
            .NotNull().WithMessage("Priority is required. Valid values: Low, Medium, High, Critical.")
            .IsInEnum().WithMessage("Priority is invalid. Valid values: Low, Medium, High, Critical.");

        RuleFor(x => x.Status)
            .NotNull().WithMessage("Status is required. Valid values: Open, InProgress, Complete, Cancelled.")
            .IsInEnum().WithMessage("Status is invalid. Valid values: Open, InProgress, Complete, Cancelled.");

        RuleFor(x => x.BuildingId)
            .NotEmpty().WithMessage("Building ID is required.");

        // Apartment, vendor and equipment are optional: common-area (building-level) requests
        // have no apartment/vendor/equipment. Matches the service layer and bulk-upload path.

        RuleFor(x => x.EstimatedCost)
            .NotNull().WithMessage("Estimated cost is required.")
            .GreaterThanOrEqualTo(0).WithMessage("Estimated cost must be greater than or equal to 0.");

        RuleFor(x => x.AnnualCost)
            .GreaterThanOrEqualTo(0).WithMessage("Annual cost must be greater than or equal to 0.");

        RuleFor(x => x.ScheduledDate)
            .NotEmpty().WithMessage("Scheduled date is required.");

        RuleFor(x => x.RecurrenceFrequency)
            .IsInEnum().WithMessage("Recurrence frequency is invalid. Valid values: Daily, Weekly, BiWeekly, Monthly, BiMonthly, Quarterly, HalfYearly, Yearly, BiYearly.")
            .When(x => x.RecurrenceFrequency.HasValue);

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required when a recurrence frequency is provided.")
            .When(x => x.RecurrenceFrequency.HasValue);
    }
}
