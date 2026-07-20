using CleanArchitecture.Shared.Models.Owner;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class OwnerUpdateRequestValidation : AbstractValidator<OwnerUpdateRequest>
{
    public OwnerUpdateRequestValidation()
    {
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

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.IdType)
            .IsInEnum().WithMessage("A valid ID type is required.");

        RuleFor(x => x.IdNumber)
            .NotEmpty().WithMessage("ID number is required.")
            .MaximumLength(100).WithMessage("ID number must not exceed 100 characters.");

        RuleFor(x => x.BankName)
            .NotEmpty().WithMessage("Bank name is required.")
            .MaximumLength(255).WithMessage("Bank name must not exceed 255 characters.");

        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number is required.")
            .MaximumLength(100).WithMessage("Account number must not exceed 100 characters.");

        RuleFor(x => x.IfscCode)
            .NotEmpty().WithMessage("IFSC code is required.")
            .MaximumLength(50).WithMessage("IFSC code must not exceed 50 characters.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid owner status is required.");
    }
}
 