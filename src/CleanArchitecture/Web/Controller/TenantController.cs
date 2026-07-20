using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/tenants")]
public class TenantController(ITenantService tenantService, ITenantMoveOutService tenantMoveOutService) : BaseController
{
    private readonly ITenantService _tenantService = tenantService;
    private readonly ITenantMoveOutService _tenantMoveOutService = tenantMoveOutService;

    /// <summary>
    /// [T-01] Retrieves a paginated list of tenants with optional search and filters.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "List of tenants retrieved successfully.", typeof(PaginatedList<TenantViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<PaginatedList<TenantViewModel>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? buildingId = null,
        [FromQuery] Guid? apartmentId = null,
        [FromQuery] TenantStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantService.GetPaginated(page, pageSize, search, buildingId, apartmentId, status, cancellationToken);
        return Ok(tenants);
    }

    /// <summary>
    /// [T-02] Retrieves single tenant details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [SwaggerResponse(200, "Tenant details retrieved successfully.", typeof(TenantViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Tenant not found.")]
    public async Task<ActionResult<TenantViewModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetById(id, cancellationToken);
        return Ok(tenant);
    }

    /// <summary>
    /// [T-03] Creates a new tenant (move in).
    /// </summary>
    [HttpPost]
    [SwaggerResponse(200, "Tenant created successfully.")]
    [SwaggerResponse(400, "Invalid request or email/ID number already exists.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> Create([FromBody] TenantCreateRequest request, CancellationToken cancellationToken)
    {
        await _tenantService.Create(request, cancellationToken);
        return Ok(new { message = "Tenant created successfully." });
    }

    /// <summary>
    /// [T-04] Updates details of an existing tenant.
    /// </summary>
    [HttpPut("{id:guid}")]
    [SwaggerResponse(200, "Tenant updated successfully.")]
    [SwaggerResponse(400, "Invalid request or duplicate email/ID number.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Tenant not found.")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TenantUpdateRequest request, CancellationToken cancellationToken)
    {
        await _tenantService.Update(id, request, cancellationToken);
        return Ok(new { message = "Tenant updated successfully." });
    }

    /// <summary>
    /// [T-05] Soft-deletes a tenant.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [SwaggerResponse(200, "Tenant deleted successfully.")]
    [SwaggerResponse(400, "Invalid admin password.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Tenant not found.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _tenantService.Delete(id, cancellationToken);
        return Ok(new { message = "Tenant deleted successfully." });
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // TENANT MOVE-OUT RECORDS ENDPOINTS
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// [TMO-01] Creates a move-out record for a tenant.
    /// </summary>
    [HttpPost("{id:guid}/move-out")]
    [SwaggerResponse(200, "Move-out record created successfully.")]
    [SwaggerResponse(400, "Invalid request or move-out record already exists.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Tenant not found.")]
    public async Task<IActionResult> CreateMoveOut(Guid id, [FromBody] TenantMoveOutCreateRequest request, CancellationToken cancellationToken)
    {
        await _tenantMoveOutService.CreateMoveOut(id, request, cancellationToken);
        return Ok(new { message = "Move-out record created successfully." });
    }

    /// <summary>
    /// [TMO-02] Retrieves move-out record for a tenant.
    /// </summary>
    [HttpGet("{id:guid}/move-out-records")]
    [HttpGet("{id:guid}/move-out")]
    [SwaggerResponse(200, "Move-out record retrieved successfully.", typeof(TenantMoveOutRecordViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Move-out record not found.")]
    public async Task<ActionResult<TenantMoveOutRecordViewModel>> GetMoveOut(Guid id, CancellationToken cancellationToken)
    {
        var record = await _tenantMoveOutService.GetByTenantId(id, cancellationToken);
        return Ok(record);
    }

    /// <summary>
    /// [TMO-03] Updates move-out record for a tenant.
    /// </summary>
    [HttpPut("{id:guid}/move-out")]
    [SwaggerResponse(200, "Move-out record updated successfully.")]
    [SwaggerResponse(400, "Invalid request.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Move-out record not found.")]
    public async Task<IActionResult> UpdateMoveOut(Guid id, [FromBody] TenantMoveOutUpdateRequest request, CancellationToken cancellationToken)
    {
        await _tenantMoveOutService.UpdateMoveOut(id, request, cancellationToken);
        return Ok(new { message = "Move-out record updated successfully." });
    }

    /// <summary>
    /// [TMO-04] Soft-deletes a move-out record for a tenant.
    /// </summary>
    [HttpDelete("{id:guid}/move-out")]
    [SwaggerResponse(200, "Move-out record deleted successfully.")]
    [SwaggerResponse(400, "Invalid admin password.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Move-out record not found.")]
    public async Task<IActionResult> DeleteMoveOut(Guid id, CancellationToken cancellationToken)
    {
        await _tenantMoveOutService.DeleteMoveOut(id, cancellationToken);
        return Ok(new { message = "Move-out record deleted successfully." });
    }
}
