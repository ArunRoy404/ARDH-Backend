using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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

    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.admin;

    [Required]
    public string Permissions { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("avatarURL")]
    public string AvatarUrl { get; set; } = string.Empty;
}
