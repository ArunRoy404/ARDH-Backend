using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Utilities;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Owner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/owners")]
public class OwnerController(IOwnerService ownerService, IUnitOfWork unitOfWork) : BaseController
{
    private readonly IOwnerService _ownerService = ownerService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// [O-01] Retrieves a paginated, filtered list of all owners.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "List of owners retrieved successfully.", typeof(PaginatedList<OwnerViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<PaginatedList<OwnerViewModel>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] OwnerStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var owners = await _ownerService.GetPaginated(page, pageSize, search, status, cancellationToken);
        return Ok(owners);
    }

    /// <summary>
    /// [O-02] Retrieves owner details by ID.
    /// </summary>
    [HttpGet("{id}")]
    [SwaggerResponse(200, "Owner details retrieved successfully.", typeof(OwnerViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Owner not found.")]
    public async Task<ActionResult<OwnerViewModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var owner = await _ownerService.GetById(id, cancellationToken);
        return Ok(owner);
    }

    /// <summary>
    /// [O-03] Creates a new owner.
    /// </summary>
    [HttpPost]
    [SwaggerResponse(200, "Owner created successfully.")]
    [SwaggerResponse(400, "Invalid request or email/ID number already exists.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> Create([FromBody] OwnerCreateRequest request, CancellationToken cancellationToken)
    {
        await _ownerService.Create(request, cancellationToken);
        return Ok(new { message = "Owner created successfully." });
    }

    /// <summary>
    /// [O-04] Updates the details of an existing owner.
    /// </summary>
    [HttpPut("{id}")]
    [SwaggerResponse(200, "Owner updated successfully.")]
    [SwaggerResponse(400, "Invalid request or email/ID number already exists.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Owner not found.")]
    public async Task<IActionResult> Update(Guid id, [FromBody] OwnerUpdateRequest request, CancellationToken cancellationToken)
    {
        await _ownerService.Update(id, request, cancellationToken);
        return Ok(new { message = "Owner updated successfully." });
    }

    /// <summary>
    /// [O-05] Soft-deletes an owner by ID.
    /// </summary>
    [HttpDelete("{id}")]
    [SwaggerResponse(200, "Owner deleted successfully.")]
    [SwaggerResponse(400, "Invalid Admin password.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Owner not found.")]
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

        await _ownerService.Delete(id, cancellationToken);
        return Ok(new { message = "Owner deleted successfully." });
    }
}
