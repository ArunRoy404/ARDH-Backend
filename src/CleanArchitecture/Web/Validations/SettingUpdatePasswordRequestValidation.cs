using CleanArchitecture.Shared.Models.Setting;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class SettingUpdatePasswordRequestValidation : AbstractValidator<SettingUpdatePasswordRequest>
{
    public SettingUpdatePasswordRequestValidation()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(6).WithMessage("New password must be at least 6 characters.");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Confirm new password is required.")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}
