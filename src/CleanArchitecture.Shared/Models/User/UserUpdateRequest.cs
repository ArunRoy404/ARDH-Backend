using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.User;

public class UserUpdateRequest
{
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    public string? Address { get; set; }

    [Required]
    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Permissions { get; set; }

    [JsonPropertyName("avatarURL")]
    public string? AvatarUrl { get; set; }
}
