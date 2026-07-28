using CleanArchitecture.Shared.Models.Tenant;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class TenantCreateRequestValidation : AbstractValidator<TenantCreateRequest>
{
    public TenantCreateRequestValidation()
    {
        RuleFor(x => x.BuildingId)
            .NotEmpty().WithMessage("Building ID is required.");

        RuleFor(x => x.ApartmentId)
            .NotEmpty().WithMessage("Apartment ID is required.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(255).WithMessage("Full name must not exceed 255 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number must not exceed 50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255).WithMessage("Email address must not exceed 255 characters.");

        RuleFor(x => x.IdType)
            .IsInEnum().WithMessage("A valid ID type is required.");

        RuleFor(x => x.IdNumber)
            .NotEmpty().WithMessage("ID number is required.")
            .MaximumLength(100).WithMessage("ID number must not exceed 100 characters.");

        RuleFor(x => x.MoveInDate)
            .NotEmpty().WithMessage("Move-in date is required.");

        RuleFor(x => x.LeaseStartDate)
            .NotEmpty().WithMessage("Lease start date is required.");

        RuleFor(x => x.MonthlyRent)
            .GreaterThanOrEqualTo(0).WithMessage("Monthly rent must be greater than or equal to 0.");

        RuleFor(x => x.SecurityDeposit)
            .GreaterThanOrEqualTo(0).WithMessage("Security deposit must be greater than or equal to 0.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid tenant status is required.");
    }
}
