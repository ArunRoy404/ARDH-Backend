using CleanArchitecture.Shared.Models.Vendor;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class VendorUpdateRequestValidation : AbstractValidator<VendorUpdateRequest>
{
    public VendorUpdateRequestValidation()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Vendor name is required.")
            .MaximumLength(255).WithMessage("Vendor name must not exceed 255 characters.");

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(255).WithMessage("Company name must not exceed 255 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number must not exceed 50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255).WithMessage("Email address must not exceed 255 characters.");

        RuleFor(x => x.VendorType)
            .IsInEnum().WithMessage("A valid vendor type is required.");

        RuleFor(x => x.GstNumber)
            .NotEmpty().WithMessage("GST number is required.")
            .MaximumLength(50).WithMessage("GST number must not exceed 50 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid vendor status is required.");
    }
}
