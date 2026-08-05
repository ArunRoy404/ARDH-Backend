using CleanArchitecture.Shared.Models.Vendor;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class VendorCreateRequestValidation : AbstractValidator<VendorCreateRequest>
{
    public VendorCreateRequestValidation()
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
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("A valid email address is required.")
            .MaximumLength(255).WithMessage("Email address must not exceed 255 characters.");

        RuleFor(x => x.VendorType)
            .NotEmpty().WithMessage("Vendor type is required.")
            .MaximumLength(100).WithMessage("Vendor type must not exceed 100 characters.");

        RuleFor(x => x.GstNumber)
            .MaximumLength(50).WithMessage("GST number must not exceed 50 characters.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid vendor status is required.");
    }
}
