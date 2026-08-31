using CleanArchitecture.Shared.Models.User;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class UpdateProfilePictureRequestValidation : AbstractValidator<UpdateProfilePictureRequest>
{
    public UpdateProfilePictureRequestValidation()
    {
        RuleFor(x => x.AvatarUrl)
            .NotEmpty().WithMessage("Avatar URL is required.")
            .Must(x => Uri.TryCreate(x, UriKind.Absolute, out _)).WithMessage("Avatar URL must be a valid URL.");
    }
}
