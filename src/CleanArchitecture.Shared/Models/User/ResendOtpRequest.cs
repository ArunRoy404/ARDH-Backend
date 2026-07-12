using System.ComponentModel.DataAnnotations;

namespace CleanArchitecture.Shared.Models.User;

public class ResendOtpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
