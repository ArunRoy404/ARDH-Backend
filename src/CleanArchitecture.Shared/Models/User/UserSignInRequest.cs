using System.ComponentModel.DataAnnotations;

namespace CleanArchitecture.Shared.Models.User;

public class UserSignInRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}