using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Apartment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/apartments")]
public class ApartmentController(IApartmentService apartmentService, IUnitOfWork unitOfWork) : BaseController
{
    private readonly IApartmentService _apartmentService = apartmentService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// [AP-01] List all apartments with pagination, search, and filters.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "List of apartments retrieved successfully.", typeof(PaginatedList<ApartmentViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<PaginatedList<ApartmentViewModel>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? buildingId = null,
        [FromQuery] Guid? ownerId = null,
        [FromQuery] ApartmentType? apartmentType = null,
        CancellationToken cancellationToken = default)
    {
        var apartments = await _apartmentService.GetPaginated(page, pageSize, search, buildingId, ownerId, apartmentType, cancellationToken);
        return Ok(apartments);
    }

    /// <summary>
    /// [AP-02] Retrieves single apartment details by ID.
    /// </summary>
    [HttpGet("{id}")]
    [SwaggerResponse(200, "Apartment details retrieved successfully.", typeof(ApartmentViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Apartment not found.")]
    public async Task<ActionResult<ApartmentViewModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var apartment = await _apartmentService.GetById(id, cancellationToken);
        return Ok(apartment);
    }

    /// <summary>
    /// [AP-03] Creates a new apartment.
    /// </summary>
    [HttpPost]
    [SwaggerResponse(200, "Apartment created successfully.")]
    [SwaggerResponse(400, "Invalid request or building/owner/flat number invalid.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> Create([FromBody] ApartmentCreateRequest request, CancellationToken cancellationToken)
    {
        await _apartmentService.Create(request, cancellationToken);
        return Ok(new { message = "Apartment created successfully." });
    }

    /// <summary>
    /// [AP-04] Updates details of an existing apartment.
    /// </summary>
    [HttpPut("{id}")]
    [SwaggerResponse(200, "Apartment updated successfully.")]
    [SwaggerResponse(400, "Invalid request or building/owner/flat number invalid.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Apartment not found.")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ApartmentUpdateRequest request, CancellationToken cancellationToken)
    {
        await _apartmentService.Update(id, request, cancellationToken);
        return Ok(new { message = "Apartment updated successfully." });
    }

    /// <summary>
    /// [AP-05] Soft-deletes an apartment by ID.
    /// </summary>
    [HttpDelete("{id}")]
    [SwaggerResponse(200, "Apartment deleted successfully.")]
    [SwaggerResponse(400, "Invalid Admin password.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Apartment not found.")]
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
        if (settings == null || string.IsNullOrEmpty(password) || !StringHelper.Verify(password, settings.AdminPassword))
        {
            return BadRequest(new { message = "Invalid Admin password." });
        }

        await _apartmentService.Delete(id, cancellationToken);
        return Ok(new { message = "Apartment deleted successfully." });
    }
}
