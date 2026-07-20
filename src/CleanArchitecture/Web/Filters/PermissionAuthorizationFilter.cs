using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Shared.Models.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CleanArchitecture.Web.Filters;

public class PermissionAuthorizationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var path = httpContext.Request.Path.Value ?? string.Empty;

            // Allow auth endpoints (sign-in, forgot-password, reset-password, logout, profile, etc.)
            if (path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            // Allow endpoints marked with [AllowAnonymous]
            var endpoint = httpContext.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            {
                await next();
                return;
            }

            // Extract user role
            var roleClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value ?? string.Empty;
            var isViewer = user.IsInRole("viewer") || roleClaim.Equals("viewer", StringComparison.OrdinalIgnoreCase);
            var isAdminRole = user.IsInRole("admin") || roleClaim.Equals("admin", StringComparison.OrdinalIgnoreCase);
            var isPropertyManagerRole = user.IsInRole("property_manager") || roleClaim.Equals("property_manager", StringComparison.OrdinalIgnoreCase);

            // Extract user permissions
            var permissionsClaim = user.Claims.FirstOrDefault(c => c.Type == "permissions")?.Value ?? string.Empty;
            var permissionList = permissionsClaim.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(p => p.Trim().ToLowerInvariant())
                                                 .ToList();

            var hasAdminPermission = isAdminRole || permissionList.Contains("admin");
            var hasPropertyPermission = isPropertyManagerRole || permissionList.Contains("properties") || permissionList.Contains("property") || hasAdminPermission;

            // Route Permission Check (Applies to ALL HTTP methods including GET)
            if (path.StartsWith("/api/users", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/settings", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/deleted-history", StringComparison.OrdinalIgnoreCase))
            {
                if (!hasAdminPermission)
                {
                    context.Result = CreateForbiddenResult("Access denied. Admin permission required for this route.");
                    return;
                }
            }
            else if (path.StartsWith("/api/buildings", StringComparison.OrdinalIgnoreCase) ||
                     path.StartsWith("/api/owners", StringComparison.OrdinalIgnoreCase) ||
                     path.StartsWith("/api/apartments", StringComparison.OrdinalIgnoreCase) ||
                     path.StartsWith("/api/tenants", StringComparison.OrdinalIgnoreCase))
            {
                if (!hasPropertyPermission)
                {
                    context.Result = CreateForbiddenResult("Access denied. Property permission required for this route.");
                    return;
                }
            }
            else if (path.StartsWith("/api/upload", StringComparison.OrdinalIgnoreCase))
            {
                if (!hasAdminPermission && !hasPropertyPermission)
                {
                    context.Result = CreateForbiddenResult("Access denied. Permission required to access upload services.");
                    return;
                }
            }

            // Viewer Role Constraint: Viewer cannot perform POST, PUT, PATCH, or DELETE operations
            var method = httpContext.Request.Method;
            var isMutatingMethod = HttpMethods.IsPost(method) ||
                                   HttpMethods.IsPut(method) ||
                                   HttpMethods.IsPatch(method) ||
                                   HttpMethods.IsDelete(method);

            if (isViewer && isMutatingMethod)
            {
                context.Result = CreateForbiddenResult("Users with 'viewer' role only have view (GET) permissions.");
                return;
            }
        }

        await next();
    }

    private static ObjectResult CreateForbiddenResult(string message)
    {
        var errorCode = $"{ApplicationConstants.Name}.{ErrorRespondCode.UNAUTHORIZED}";
        var error = new Error(errorCode, message);
        return new ObjectResult(error)
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
