using CleanArchitecture.Shared.Models.Maintenance;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class MaintenanceRequestStatusUpdateRequestValidation : AbstractValidator<MaintenanceRequestStatusUpdateRequest>
{
    public MaintenanceRequestStatusUpdateRequestValidation()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid status is required.");
    }
}
