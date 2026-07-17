using CleanArchitecture.Shared.Models.User;
using CleanArchitecture.Shared.Domain.Enums;
using FluentValidation;
using System;
using System.Linq;

namespace CleanArchitecture.Web.Validations;

public class UserUpdateRequestValidation : AbstractValidator<UserUpdateRequest>
{
    public UserUpdateRequestValidation()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("A valid role is required.");

        RuleFor(x => x.Permissions)
            .NotEmpty().WithMessage("Permissions are required.")
            .Must(BeValidPermissions).WithMessage("Permissions must only contain: dashboard, properties, finance, operations, admin.");

        RuleFor(x => x.AvatarUrl)
            .NotEmpty().WithMessage("Avatar URL is required.")
            .MaximumLength(500).WithMessage("Avatar URL must not exceed 500 characters.");
    }

    private bool BeValidPermissions(string permissions)
    {
        if (string.IsNullOrWhiteSpace(permissions))
            return false;

        var parts = permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        if (parts.Length == 0)
            return false;

        return parts.All(p => Enum.TryParse<UserPermission>(p, out _));
    }
}
