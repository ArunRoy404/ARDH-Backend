using CleanArchitecture.Shared.Domain.Enums;

namespace CleanArchitecture.Shared.Models.User;

public class UserSignInResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Token { get; set; } = string.Empty;
}
