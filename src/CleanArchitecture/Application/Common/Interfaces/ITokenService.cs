using System.Security.Claims;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
    ClaimsPrincipal ValidateToken(string token);
}
