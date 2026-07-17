using CleanArchitecture.Application;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Building;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/buildings")]
public class BuildingController(IBuildingService buildingService, IUnitOfWork unitOfWork) : BaseController
{
    private readonly IBuildingService _buildingService = buildingService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// [B-01] Retrieves a paginated, filtered list of all buildings.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "List of buildings retrieved successfully.", typeof(PaginatedList<BuildingViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<PaginatedList<BuildingViewModel>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] BuildingStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var buildings = await _buildingService.GetPaginated(page, pageSize, search, status, cancellationToken);
        return Ok(buildings);
    }

    /// <summary>
    /// [B-02] Retrieves building details by ID.
    /// </summary>
    [HttpGet("{id}")]
    [SwaggerResponse(200, "Building details retrieved successfully.", typeof(BuildingViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Building not found.")]
    public async Task<ActionResult<BuildingViewModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var building = await _buildingService.GetById(id, cancellationToken);
        return Ok(building);
    }

    /// <summary>
    /// [B-03] Creates a new building.
    /// </summary>
    [HttpPost]
    [SwaggerResponse(200, "Building created successfully.")]
    [SwaggerResponse(400, "Invalid request or building name already exists.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> Create([FromBody] BuildingCreateRequest request, CancellationToken cancellationToken)
    {
        await _buildingService.Create(request, cancellationToken);
        return Ok(new { message = "Building created successfully." });
    }

    /// <summary>
    /// [B-04] Updates the details of an existing building.
    /// </summary>
    [HttpPut("{id}")]
    [SwaggerResponse(200, "Building updated successfully.")]
    [SwaggerResponse(400, "Invalid request or building name already exists.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Building not found.")]
    public async Task<IActionResult> Update(Guid id, [FromBody] BuildingUpdateRequest request, CancellationToken cancellationToken)
    {
        request.Id = id;
        await _buildingService.Update(request, cancellationToken);
        return Ok(new { message = "Building updated successfully." });
    }

    /// <summary>
    /// [B-05] Soft-deletes a building by ID.
    /// </summary>
    [HttpDelete("{id}")]
    [SwaggerResponse(200, "Building deleted successfully.")]
    [SwaggerResponse(400, "Invalid Admin password.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Building not found.")]
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

        await _buildingService.Delete(id, cancellationToken);
        return Ok(new { message = "Building deleted successfully." });
    }
}
