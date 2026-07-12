using System;
using System.Linq;
using System.Security.Claims;
using CleanArchitecture.Application.Common.Interfaces;

using Microsoft.AspNetCore.Http;

namespace CleanArchitecture.Web.Services;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Guid GetCurrentUserId()
    {
        var userIdStr = GetCurrentStringUserId();
        return Guid.TryParse(userIdStr, out var userId) ? userId : Guid.Empty;
    }

    public string GetCurrentStringUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = user?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
        return userIdClaim?.Value ?? string.Empty;
    }
}
