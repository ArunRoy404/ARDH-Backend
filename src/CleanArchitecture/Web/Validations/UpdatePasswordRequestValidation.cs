using CleanArchitecture.Shared.Models.User;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class UpdatePasswordRequestValidation : AbstractValidator<UpdatePasswordRequest>
{
    public UpdatePasswordRequestValidation()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(6).WithMessage("New password must be at least 6 characters.")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from the current password.");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Confirm new password is required.")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}
