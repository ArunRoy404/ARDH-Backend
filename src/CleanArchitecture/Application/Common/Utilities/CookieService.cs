using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Interfaces;

namespace CleanArchitecture.Application.Common.Utilities;

public class CookieService(IHttpContextAccessor httpContextAccessor) : ICookieService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    public void Set(string token, bool rememberMe = false)
    {
        var context = _httpContextAccessor.HttpContext;
        var isHttps = context?.Request.IsHttps ?? false;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Secure = isHttps
        };

        if (rememberMe)
        {
            cookieOptions.Expires = DateTimeOffset.UtcNow.AddYears(100);
        }
        else
        {
            cookieOptions.MaxAge = TimeSpan.FromHours(24);
        }

        context?.Response.Cookies.Append("token_key", token, cookieOptions);
    }

    public void Delete() => _httpContextAccessor.HttpContext?.Response.Cookies.Delete("token_key");

    public string Get()
    {
        var token = _httpContextAccessor.HttpContext?.Request.Cookies["token_key"];
        return string.IsNullOrEmpty(token) ? throw UserException.UserUnauthorizedException() : token;
    }
}
