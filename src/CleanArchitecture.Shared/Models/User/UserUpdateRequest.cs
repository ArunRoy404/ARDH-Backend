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

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Permissions { get; set; }

    /// <summary>
    /// Null means "leave unchanged" — a client that omits this field will not silently reset the
    /// user's opt-out. Only an explicit true/false in the request body changes it.
    /// </summary>
    public bool? ReceiveEmailNotifications { get; set; }

    [Required]
    [JsonPropertyName("avatarURL")]
    public string AvatarUrl { get; set; } = string.Empty;
}