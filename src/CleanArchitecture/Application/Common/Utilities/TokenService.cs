using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Domain.Entities;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CleanArchitecture.Application.Common.Utilities;

public class TokenService(AppSettings appSettings, ICurrentTime time) : ITokenService
{
    private readonly AppSettings _appSettings = appSettings;
    private readonly ICurrentTime _time = time;

    public string GenerateToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.Identity.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: _time.GetCurrentTime().AddDays(1),
            audience: _appSettings.Identity.Audience,
            issuer: _appSettings.Identity.Issuer,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        IdentityModelEventSource.ShowPII = true;
        TokenValidationParameters validationParameters = new()
        {
            ValidIssuer = _appSettings.Identity.Issuer,
            ValidAudience = _appSettings.Identity.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.Identity.Key)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);

        return principal;
    }
}
