using System;
using System.Text.Json.Serialization;
using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.User;

public class UserViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public UserRole Role { get; set; }
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("avatarURL")]
    public string? AvatarURL { get => AvatarUrl; set => AvatarUrl = value; }

    public string? City => Address;
    public bool IsActive { get; set; }
    public string? Permissions { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
