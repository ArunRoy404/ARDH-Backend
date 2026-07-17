using System.ComponentModel.DataAnnotations;

namespace CleanArchitecture.Shared.Models.Setting;

public class SettingUpdateRequest
{
    [Required]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string CompanyEmail { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string Icon { get; set; } = string.Empty;

    [Required]
    public string Fav { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Admin password must be at least 6 characters long.")]
    public string AdminPassword { get; set; } = string.Empty;
}
