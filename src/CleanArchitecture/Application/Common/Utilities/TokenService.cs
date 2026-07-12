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

    public string GenerateToken(User user, bool rememberMe = false)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.Identity.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("remember_me", rememberMe.ToString().ToLower())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: rememberMe ? _time.GetCurrentTime().AddYears(100) : _time.GetCurrentTime().AddHours(24),
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
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);

        return principal;
    }
}
