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

            // Extract user permissions
            var permissionsClaim = user.Claims.FirstOrDefault(c => c.Type == "permissions")?.Value ?? string.Empty;
            var permissionList = permissionsClaim.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(p => p.Trim().ToLowerInvariant())
                                                 .ToList();

            // Admins always pass every module check below; every other role's access is driven
            // strictly by their resolved Permissions claim (defaults + whatever extra modules an
            // admin explicitly granted them) - there is no role-based bypass for any other role.
            var hasAdminPermission = isAdminRole || permissionList.Contains("admin");
            bool HasModule(string module) => hasAdminPermission || permissionList.Contains(module);

            // GET requests to these data modules are readable by any authenticated user,
            // regardless of permission. Only mutating methods (POST/PUT/PATCH/DELETE) are
            // permission-gated below. Every other module (users, settings, deleted-history,
            // upload, notifications, dashboard/activities) keeps requiring permission on GET too.
            var isGetMethod = HttpMethods.IsGet(httpContext.Request.Method);
            var isOpenReadModule = path.StartsWith("/api/buildings", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/api/owners", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/api/apartments", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/api/tenants", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/api/vendors", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/api/equipment", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/api/amc-contracts", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/api/maintenance", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/api/income", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/api/reports", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/api/expenses", StringComparison.OrdinalIgnoreCase);

            var skipModulePermissionCheck = isGetMethod && isOpenReadModule;

            // Route Permission Check (Applies to ALL HTTP methods including GET, except the
            // open-read modules above on GET). Each route now requires its own specific module
            // permission rather than a coarse bucket, so e.g. a property_manager only gets
            // 'buildings' if it was explicitly granted on top of their operations defaults.
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
            else if (path.StartsWith("/api/buildings", StringComparison.OrdinalIgnoreCase))
            {
                if (!skipModulePermissionCheck && !HasModule("buildings"))
                {
                    context.Result = CreateForbiddenResult("Access denied. 'buildings' permission required for this route.");
                    return;
                }
            }
            else if (path.StartsWith("/api/owners", StringComparison.OrdinalIgnoreCase))
            {
                if (!skipModulePermissionCheck && !HasModule("owners"))
                {
                    context.Result = CreateForbiddenResult("Access denied. 'owners' permission required for this route.");
                    return;
                }
            }
            else if (path.StartsWith("/api/apartments", StringComparison.OrdinalIgnoreCase))
            {
                if (!skipModulePermissionCheck && !HasModule("apartments"))
                {
                    context.Result = CreateForbiddenResult("Access denied. 'apartments' permission required for this route.");
                    return;
                }
            }
            else if (path.StartsWith("/api/tenants", StringComparison.OrdinalIgnoreCase))
            {
                if (!skipModulePermissionCheck && !HasModule("tenants"))
                {
                    context.Result = CreateForbiddenResult("Access denied. 'tenants' permission required for this route.");
                    return;
                }
            }
            else if (path.StartsWith("/api/vendors", StringComparison.OrdinalIgnoreCase))
            {
                if (!skipModulePermissionCheck && !HasModule("vendors"))
                {
                    context.Result = CreateForbiddenResult("Access denied. 'vendors' permission required for this route.");
                    return;
                }
            }
            else if (path.StartsWith("/api/equipment", StringComparison.OrdinalIgnoreCase))
            {
                if (!skipModulePermissionCheck && !HasModule("equipment"))
                {
                    context.Result = CreateForbiddenResult("Access denied. 'equipment' permission required for this route.");
                    return;
                }
            }
            else if (path.StartsWith("/api/amc-contracts", StringComparison.OrdinalIgnoreCase))
            {
                if (!skipModulePermissionCheck && !HasModule("amc_contracts"))
                {
                    context.Result = CreateForbiddenResult("Access denied. 'amc_contracts' permission required for this route.");
                    return;
                }
            }
            else if (path.StartsWith("/api/maintenance", StringComparison.OrdinalIgnoreCase))
            {
                if (!skipModulePermissionCheck && !HasModule("maintenance"))
                {
                    context.Result = CreateForbiddenResult("Access denied. 'maintenance' permission required for this route.");
                    return;
                }
            }
            else if (path.StartsWith("/api/income", StringComparison.OrdinalIgnoreCase))
            {
                if (!skipModulePermissionCheck && !HasModule("income"))
                {
                    context.Result = CreateForbiddenResult("Access denied. 'income' permission required for this route.");
                    return;
                }
            }
            else if (path.StartsWith("/api/reports", StringComparison.OrdinalIgnoreCase))
            {
                if (!skipModulePermissionCheck && !HasModule("reports"))
                {
                    context.Result = CreateForbiddenResult("Access denied. 'reports' permission required for this route.");
                    return;
                }
            }
            else if (path.StartsWith("/api/expenses", StringComparison.OrdinalIgnoreCase))
            {
                if (!skipModulePermissionCheck && !HasModule("expenses"))
                {
                    context.Result = CreateForbiddenResult("Access denied. 'expenses' permission required for this route.");
                    return;
                }
            }
            else if (path.StartsWith("/api/upload", StringComparison.OrdinalIgnoreCase) ||
                     path.StartsWith("/api/notifications", StringComparison.OrdinalIgnoreCase))
            {
                // File upload/delete (image, document, xlsx, id-proof) is a generic utility used
                // across every module's forms, and notifications are personal to the signed-in
                // user - both are open to any authenticated user regardless of permission. The
                // viewer-role write block below still applies, since viewer is meant to be
                // read-only.
            }
            else if (path.StartsWith("/api/activities", StringComparison.OrdinalIgnoreCase) ||
                     path.StartsWith("/api/dashboard", StringComparison.OrdinalIgnoreCase))
            {
                if (!HasModule("dashboard"))
                {
                    context.Result = CreateForbiddenResult("Access denied. 'dashboard' permission required for this route.");
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
