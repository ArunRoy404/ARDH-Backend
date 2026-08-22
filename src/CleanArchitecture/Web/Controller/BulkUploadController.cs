using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models.BulkUpload;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/bulk-upload")]
public class BulkUploadController(IBulkUploadService bulkUploadService) : BaseController
{
    private readonly IBulkUploadService _bulkUploadService = bulkUploadService;

    /// <summary>
    /// [BU-01] Starts a bulk upload for a module. The file must first be uploaded via
    /// POST /api/upload/xlsx; pass the returned URL here. Processing happens in the background —
    /// poll the status endpoints for the result.
    /// </summary>
    [HttpPost]
    [SwaggerResponse(200, "Bulk upload started successfully.", typeof(BulkUploadViewModel))]
    [SwaggerResponse(400, "Invalid module or missing file URL.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied for this module.")]
    public async Task<ActionResult<BulkUploadViewModel>> Start([FromBody] BulkUploadStartRequest request, CancellationToken cancellationToken)
    {
        if (!HasModulePermission(request.Module))
        {
            return Forbid();
        }

        var result = await _bulkUploadService.StartAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [BU-02] Returns all bulk uploads, optionally filtered by module (newest first).
    /// </summary>
    [HttpGet("status")]
    [SwaggerResponse(200, "List of bulk uploads retrieved successfully.", typeof(List<BulkUploadViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied for this module.")]
    public async Task<ActionResult<List<BulkUploadViewModel>>> GetStatus(
        [FromQuery] BulkUploadModule? module = null,
        CancellationToken cancellationToken = default)
    {
        if (module.HasValue && !HasModulePermission(module.Value))
        {
            return Forbid();
        }

        var result = await _bulkUploadService.GetStatusAsync(module, cancellationToken);

        // Non-admin users only see jobs for modules they have permission for and created by themselves.
        if (!IsBypassUser())
        {
            var allowed = PermittedModules();
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid" || c.Type == "sub")?.Value;
            Guid.TryParse(userIdClaim, out var currentUserId);

            result = result.Where(x => allowed.Contains(x.Module) && (!x.CreatedBy.HasValue || x.CreatedBy == currentUserId)).ToList();
        }

        return Ok(result);
    }

    /// <summary>
    /// [BU-03] Returns a single bulk upload job by ID.
    /// </summary>
    [HttpGet("status/{id:guid}")]
    [SwaggerResponse(200, "Bulk upload details retrieved successfully.", typeof(BulkUploadViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Bulk upload not found.")]
    public async Task<ActionResult<BulkUploadViewModel>> GetStatusById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bulkUploadService.GetStatusByIdAsync(id, cancellationToken);

        // Per-module permission applies to the job's module as well, and non-admin users can only view their own jobs.
        if (!IsBypassUser())
        {
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid" || c.Type == "sub")?.Value;
            Guid.TryParse(userIdClaim, out var currentUserId);

            if (!HasModulePermission(result.Module) || (result.CreatedBy.HasValue && result.CreatedBy != currentUserId))
            {
                return Forbid();
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// [BU-04] Downloads an XLSX template for the given module (headers + one sample row
    /// matching the create API fields exactly).
    /// </summary>
    [HttpGet("template")]
    [SwaggerResponse(200, "XLSX template downloaded successfully.")]
    [SwaggerResponse(400, "Invalid module.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied for this module.")]
    public async Task<IActionResult> GetTemplate(
        [FromQuery] BulkUploadModule module,
        CancellationToken cancellationToken = default)
    {
        if (!HasModulePermission(module))
        {
            return Forbid();
        }

        var bytes = await _bulkUploadService.GetTemplateAsync(module, cancellationToken);
        return File(bytes, CleanArchitecture.Application.Common.Utilities.XlsxHelper.ContentType, $"{module.ToString().ToLowerInvariant()}_bulk_upload_template.xlsx");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Per-module permission check (mirrors the PermissionAuthorizationFilter).
    // Each BulkUploadModule now maps 1:1 to its UserPermission module - no bucket
    // grouping needed. Only admin bypasses; property_manager's access is driven
    // purely by their resolved Permissions claim like every other non-admin role.
    // ─────────────────────────────────────────────────────────────────────────

    private static string? ModulePermissionName(BulkUploadModule module) => module switch
    {
        BulkUploadModule.Apartments => "apartments",
        BulkUploadModule.Tenants => "tenants",
        BulkUploadModule.Owners => "owners",
        BulkUploadModule.Income => "income",
        BulkUploadModule.Expenses => "expenses",
        BulkUploadModule.Maintenance => "maintenance",
        BulkUploadModule.Equipment => "equipment",
        _ => null
    };

    private bool HasModulePermission(BulkUploadModule module)
    {
        var user = HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (IsBypassUser())
        {
            return true;
        }

        var permissionList = (user.Claims.FirstOrDefault(c => c.Type == "permissions")?.Value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToLowerInvariant())
            .ToList();

        var permissionName = ModulePermissionName(module);
        return permissionName != null && permissionList.Contains(permissionName);
    }

    /// <summary>Admin bypass for per-module filtering.</summary>
    private bool IsBypassUser()
    {
        var user = HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var roleClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value ?? string.Empty;
        var permissionList = (user.Claims.FirstOrDefault(c => c.Type == "permissions")?.Value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToLowerInvariant())
            .ToList();

        return user.IsInRole("admin") || roleClaim.Equals("admin", StringComparison.OrdinalIgnoreCase) || permissionList.Contains("admin");
    }

    /// <summary>Modules the current user may view, based on their permission claims.</summary>
    private HashSet<BulkUploadModule> PermittedModules()
    {
        var user = HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return [];
        }

        var permissionList = (user.Claims.FirstOrDefault(c => c.Type == "permissions")?.Value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToLowerInvariant())
            .ToList();

        var modules = new HashSet<BulkUploadModule>();
        foreach (var module in Enum.GetValues<BulkUploadModule>())
        {
            var permissionName = ModulePermissionName(module);
            if (permissionName != null && permissionList.Contains(permissionName))
            {
                modules.Add(module);
            }
        }

        return modules;
    }
}
