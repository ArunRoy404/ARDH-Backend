using System.ComponentModel.DataAnnotations;

namespace CleanArchitecture.Shared.Models.User;

public class VerifyOtpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string OTP { get; set; } = string.Empty;
}
