using CleanArchitecture.Shared.Models.Setting;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class SettingUpdateRequestValidation : AbstractValidator<SettingUpdateRequest>
{
    public SettingUpdateRequestValidation()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(255).WithMessage("Company name must not exceed 255 characters.");

        RuleFor(x => x.CompanyEmail)
            .NotEmpty().WithMessage("Company email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255).WithMessage("Company email must not exceed 255 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.");

        RuleFor(x => x.Icon)
            .NotEmpty().WithMessage("Icon is required.");

        RuleFor(x => x.Fav)
            .NotEmpty().WithMessage("Favicon (fav) is required.");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Current admin password is required.");
    }
}
