using CleanArchitecture.Shared.Models.User;
using FluentValidation;

namespace CleanArchitecture.Web.Validations;

public class UserSignInRequestValidation : AbstractValidator<UserSignInRequest>
{
    public UserSignInRequestValidation()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
