using System.ComponentModel.DataAnnotations;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.User;

public class UserCreateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? Address { get; set; }

    [Required]
    public UserRole Role { get; set; } = UserRole.Viewer;

    public string? Permissions { get; set; }
}
