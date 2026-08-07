using CleanArchitecture.Shared.Models.Maintenance;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class MaintenanceRequestStatusUpdateRequestValidation : AbstractValidator<MaintenanceRequestStatusUpdateRequest>
{
    public MaintenanceRequestStatusUpdateRequestValidation()
    {
        RuleFor(x => x.Status)
            .NotNull().WithMessage("Status is required. Valid values: Open, InProgress, Complete, Cancelled.")
            .IsInEnum().WithMessage("A valid status is required. Valid values: Open, InProgress, Complete, Cancelled.");
    }
}
