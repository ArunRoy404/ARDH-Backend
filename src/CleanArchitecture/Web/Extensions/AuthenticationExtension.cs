using System.Text;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common;
using CleanArchitecture.Domain.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CleanArchitecture.Web.Extensions;

public static class AuthenticationExtensions
{
    public static void AddAuth(this IServiceCollection services, Identity identitySettings)
    {
        var schemeName = $"{JwtBearerDefaults.AuthenticationScheme}_{identitySettings.Issuer}";
        var authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = schemeName;
            options.DefaultChallengeScheme = schemeName;
            options.DefaultScheme = schemeName;
        });

        authenticationBuilder.AddJwtBearer(schemeName, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateLifetime = true,
                ValidateAudience = true,
                ValidIssuer = identitySettings.Issuer,
                ValidAudience = identitySettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(identitySettings.Key)),
                ValidateIssuerSigningKey = true,
            };
            options.Authority = identitySettings.Issuer;
            options.RequireHttpsMetadata = identitySettings.ValidateHttps;
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (string.IsNullOrEmpty(context.Token))
                    {
                        var jwtCookie = context.Request.Cookies["token_key"];
                        if (!string.IsNullOrEmpty(jwtCookie))
                        {
                            context.Token = jwtCookie;
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().AddAuthenticationSchemes(schemeName).Build();

            options.AddPolicy("user_read", policy => policy.Requirements.Add(
                new HasScopeRequirement(
                identitySettings.ScopeBaseDomain,
                 identitySettings.ScopeBaseDomain + "/read",
                 identitySettings.Issuer)));

            options.AddPolicy("user_write", policy => policy.Requirements.Add(
                new HasScopeRequirement(
                    identitySettings.ScopeBaseDomain,
                    identitySettings.ScopeBaseDomain + "/write",
                    identitySettings.Issuer)));
        });
    }

    public static void AddAuthLocal(this IServiceCollection services, Identity identitySettings)
    {
        var schemeName = $"{JwtBearerDefaults.AuthenticationScheme}_{identitySettings.Issuer}";
        var authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = schemeName;
            options.DefaultChallengeScheme = schemeName;
            options.DefaultScheme = schemeName;
        });

        authenticationBuilder.AddJwtBearer(schemeName, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateLifetime = false,
                ValidateAudience = true,
                ValidIssuer = identitySettings.Issuer,
                ValidAudience = identitySettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(identitySettings.Key)),
                ValidateIssuerSigningKey = true,
            };
            options.Authority = identitySettings.Issuer;
            options.RequireHttpsMetadata = identitySettings.ValidateHttps;
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (string.IsNullOrEmpty(context.Token))
                    {
                        var jwtCookie = context.Request.Cookies["token_key"];
                        if (!string.IsNullOrEmpty(jwtCookie))
                        {
                            context.Token = jwtCookie;
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().AddAuthenticationSchemes(schemeName).Build();

            options.AddPolicy("user_read", policy => policy.Requirements.Add(
                new HasScopeRequirement(
                identitySettings.ScopeBaseDomain,
                 identitySettings.ScopeBaseDomain + "/read",
                 identitySettings.Issuer)));

            options.AddPolicy("user_write", policy => policy.Requirements.Add(
                new HasScopeRequirement(
                    identitySettings.ScopeBaseDomain,
                    identitySettings.ScopeBaseDomain + "/write",
                    identitySettings.Issuer)));
        });
    }
}
