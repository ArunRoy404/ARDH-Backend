using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/maintenance")]
public class MaintenanceController(IMaintenanceRequestService maintenanceRequestService, IUnitOfWork unitOfWork) : BaseController
{
    private readonly IMaintenanceRequestService _maintenanceRequestService = maintenanceRequestService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// [M-01] Retrieves all maintenance requests with support for pagination, search, and filters.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "List of maintenance requests retrieved successfully.", typeof(PaginatedList<MaintenanceRequestViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<PaginatedList<MaintenanceRequestViewModel>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] MaintenanceStatus? status = null,
        [FromQuery] MaintenancePriority? priority = null,
        [FromQuery] MaintenanceCategory? category = null,
        [FromQuery] Guid? buildingId = null,
        [FromQuery] Guid? vendorId = null,
        [FromQuery] Guid? equipmentId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _maintenanceRequestService.GetPaginated(
            page, pageSize, search, status, priority, category, buildingId, vendorId, equipmentId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [M-08] Retrieves maintenance request statistics.
    /// </summary>
    [HttpGet("stats")]
    [SwaggerResponse(200, "Maintenance request statistics retrieved successfully.", typeof(MaintenanceRequestStatsViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<MaintenanceRequestStatsViewModel>> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _maintenanceRequestService.GetStats(cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// [M-02] Retrieves single maintenance request details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [SwaggerResponse(200, "Maintenance request details retrieved successfully.", typeof(MaintenanceRequestViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Maintenance request not found.")]
    public async Task<ActionResult<MaintenanceRequestViewModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _maintenanceRequestService.GetById(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [M-03] Creates a new maintenance request.
    /// </summary>
    [HttpPost]
    [SwaggerResponse(200, "Maintenance request created successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> Create([FromBody] MaintenanceRequestCreateRequest request, CancellationToken cancellationToken)
    {
        await _maintenanceRequestService.Create(request, cancellationToken);
        return Ok(new { message = "Maintenance request created successfully." });
    }

    /// <summary>
    /// [M-04] Updates details of an existing maintenance request.
    /// </summary>
    [HttpPut("{id:guid}")]
    [SwaggerResponse(200, "Maintenance request updated successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Maintenance request not found.")]
    public async Task<IActionResult> Update(Guid id, [FromBody] MaintenanceRequestUpdateRequest request, CancellationToken cancellationToken)
    {
        await _maintenanceRequestService.Update(id, request, cancellationToken);
        return Ok(new { message = "Maintenance request updated successfully." });
    }

    /// <summary>
    /// [M-05] Soft-deletes a maintenance request.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [SwaggerResponse(200, "Maintenance request deleted successfully.")]
    [SwaggerResponse(400, "Invalid Admin password.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Maintenance request not found.")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(password))
        {
            if (HttpContext.Request.Headers.TryGetValue("X-Admin-Password", out var headerPassword))
            {
                password = headerPassword.ToString();
            }
        }

        var settings = await _unitOfWork.SettingRepository.FirstOrDefaultAsync(x => true);
        if (settings == null || string.IsNullOrEmpty(password) || !CleanArchitecture.Application.Common.Utilities.StringHelper.Verify(password, settings.AdminPassword))
        {
            return BadRequest(new { message = "Invalid Admin password." });
        }

        await _maintenanceRequestService.Delete(id, cancellationToken);
        return Ok(new { message = "Maintenance request deleted successfully." });
    }

    /// <summary>
    /// [M-06] Updates the status of a maintenance request.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [SwaggerResponse(200, "Maintenance request status updated successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Maintenance request not found.")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] MaintenanceRequestStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        await _maintenanceRequestService.UpdateStatus(id, request, cancellationToken);
        return Ok(new { message = "Maintenance request status updated successfully." });
    }

    /// <summary>
    /// [M-07] Assigns a vendor or staff to a maintenance request.
    /// </summary>
    [HttpPatch("{id:guid}/assign")]
    [SwaggerResponse(200, "Maintenance request assignment updated successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Maintenance request not found.")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] MaintenanceRequestAssignRequest request, CancellationToken cancellationToken)
    {
        await _maintenanceRequestService.Assign(id, request, cancellationToken);
        return Ok(new { message = "Maintenance request assignment updated successfully." });
    }
}
