using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Equipment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/equipment")]
public class EquipmentController(IEquipmentService equipmentService) : BaseController
{
    private readonly IEquipmentService _equipmentService = equipmentService;

    /// <summary>
    /// [E-01] Retrieves a paginated list of equipment with optional search and filters.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "List of equipment retrieved successfully.", typeof(PaginatedList<EquipmentViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<PaginatedList<EquipmentViewModel>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? buildingId = null,
        [FromQuery] EquipmentType? type = null,
        [FromQuery] EquipmentStatus? status = null,
        [FromQuery] Guid? amcVendorId = null,
        CancellationToken cancellationToken = default)
    {
        var equipment = await _equipmentService.GetPaginated(page, pageSize, search, buildingId, type, status, amcVendorId, cancellationToken);
        return Ok(equipment);
    }

    /// <summary>
    /// [E-02] Retrieves single equipment details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [SwaggerResponse(200, "Equipment details retrieved successfully.", typeof(EquipmentViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Equipment not found.")]
    public async Task<ActionResult<EquipmentViewModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var equipment = await _equipmentService.GetById(id, cancellationToken);
        return Ok(equipment);
    }

    /// <summary>
    /// [E-03] Creates a new equipment.
    /// </summary>
    [HttpPost]
    [SwaggerResponse(200, "Equipment created successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> Create([FromBody] EquipmentCreateRequest request, CancellationToken cancellationToken)
    {
        await _equipmentService.Create(request, cancellationToken);
        return Ok(new { message = "Equipment created successfully." });
    }

    /// <summary>
    /// [E-04] Updates details of an existing equipment.
    /// </summary>
    [HttpPut("{id:guid}")]
    [SwaggerResponse(200, "Equipment updated successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Equipment not found.")]
    public async Task<IActionResult> Update(Guid id, [FromBody] EquipmentUpdateRequest request, CancellationToken cancellationToken)
    {
        await _equipmentService.Update(id, request, cancellationToken);
        return Ok(new { message = "Equipment updated successfully." });
    }

    /// <summary>
    /// [E-05] Soft-deletes an equipment.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [SwaggerResponse(200, "Equipment deleted successfully.")]
    [SwaggerResponse(400, "Invalid admin password.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Equipment not found.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _equipmentService.Delete(id, cancellationToken);
        return Ok(new { message = "Equipment deleted successfully." });
    }

    /// <summary>
    /// [E-06] Updates equipment status only.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [SwaggerResponse(200, "Equipment status updated successfully.")]
    [SwaggerResponse(400, "Invalid status value.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Equipment not found.")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] EquipmentStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        await _equipmentService.UpdateStatus(id, request, cancellationToken);
        return Ok(new { message = "Equipment status updated successfully." });
    }
}
