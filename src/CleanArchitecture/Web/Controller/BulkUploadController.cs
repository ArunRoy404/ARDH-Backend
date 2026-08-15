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

        // Non-admin users only see the modules they have permission for.
        if (!IsBypassUser())
        {
            var allowed = PermittedModules();
            result = result.Where(x => allowed.Contains(x.Module)).ToList();
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

        // Per-module permission applies to the job's module as well.
        if (!IsBypassUser() && !HasModulePermission(result.Module))
        {
            return Forbid();
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
    // Per-module permission check (mirrors the PermissionAuthorizationFilter)
    // apartments/tenants/owners → properties; maintenance/equipment → operations;
    // income/expenses → finance. Admin + property_manager bypass everything.
    // ─────────────────────────────────────────────────────────────────────────

    private bool HasModulePermission(BulkUploadModule module)
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

        var isAdmin = user.IsInRole("admin") || roleClaim.Equals("admin", StringComparison.OrdinalIgnoreCase) || permissionList.Contains("admin");
        var isPropertyManager = user.IsInRole("property_manager") || roleClaim.Equals("property_manager", StringComparison.OrdinalIgnoreCase);

        if (isAdmin || isPropertyManager)
        {
            return true;
        }

        return module switch
        {
            BulkUploadModule.Apartments or BulkUploadModule.Tenants or BulkUploadModule.Owners =>
                permissionList.Contains("properties") || permissionList.Contains("property"),
            BulkUploadModule.Maintenance or BulkUploadModule.Equipment =>
                permissionList.Contains("operations") || permissionList.Contains("operation"),
            BulkUploadModule.Income =>
                permissionList.Contains("finance"),
            // Expenses allow finance OR operations — same as the existing /api/expenses gate.
            BulkUploadModule.Expenses =>
                permissionList.Contains("finance") || permissionList.Contains("operations") || permissionList.Contains("operation"),
            _ => false
        };
    }

    /// <summary>Admin / property_manager bypass per-module filtering.</summary>
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

        return user.IsInRole("admin") || roleClaim.Equals("admin", StringComparison.OrdinalIgnoreCase) || permissionList.Contains("admin")
            || user.IsInRole("property_manager") || roleClaim.Equals("property_manager", StringComparison.OrdinalIgnoreCase);
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

        if (permissionList.Contains("properties") || permissionList.Contains("property"))
        {
            modules.Add(BulkUploadModule.Apartments);
            modules.Add(BulkUploadModule.Tenants);
            modules.Add(BulkUploadModule.Owners);
        }

        if (permissionList.Contains("operations") || permissionList.Contains("operation"))
        {
            modules.Add(BulkUploadModule.Maintenance);
            modules.Add(BulkUploadModule.Equipment);
            modules.Add(BulkUploadModule.Expenses);
        }

        if (permissionList.Contains("finance"))
        {
            modules.Add(BulkUploadModule.Income);
            modules.Add(BulkUploadModule.Expenses);
        }

        return modules;
    }
}
