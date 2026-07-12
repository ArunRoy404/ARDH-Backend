using System;
using System.Linq;
using System.Security.Claims;
using CleanArchitecture.Application.Common.Interfaces;

namespace CleanArchitecture.Web.Services;

public class CurrentUser(ITokenService tokenService, ICookieService cookieService) : ICurrentUser
{
    private readonly ITokenService _tokenService = tokenService;
    private readonly ICookieService _cookieService = cookieService;

    public Guid GetCurrentUserId()
    {
        var userIdStr = GetCurrentStringUserId();
        return Guid.TryParse(userIdStr, out var userId) ? userId : Guid.Empty;
    }

    public string GetCurrentStringUserId()
    {
        var jwtCookie = _cookieService.Get();
        if (string.IsNullOrEmpty(jwtCookie))
        {
            return string.Empty;
        }

        var token = _tokenService.ValidateToken(jwtCookie);
        var userIdClaim = token?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);

        return userIdClaim?.Value ?? string.Empty;
    }
}
