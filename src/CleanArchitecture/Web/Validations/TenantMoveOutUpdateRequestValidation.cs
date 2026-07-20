using CleanArchitecture.Shared.Models.Tenant;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class TenantMoveOutUpdateRequestValidation : AbstractValidator<TenantMoveOutUpdateRequest>
{
    public TenantMoveOutUpdateRequestValidation()
    {
        RuleFor(x => x.MoveOutDate)
            .NotEmpty().WithMessage("Move-out date is required.");

        RuleFor(x => x.PendingRent)
            .GreaterThanOrEqualTo(0).WithMessage("Pending rent must be greater than or equal to 0.");

        RuleFor(x => x.DamageAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Damage amount must be greater than or equal to 0.");

        RuleFor(x => x.RefundAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Refund amount must be greater than or equal to 0.");

        RuleFor(x => x.IdNumber)
            .NotEmpty().WithMessage("ID number is required.")
            .MaximumLength(100).WithMessage("ID number must not exceed 100 characters.");
    }
}
