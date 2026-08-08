using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Vendor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/vendors")]
public class VendorController(IVendorService vendorService, IUnitOfWork unitOfWork) : BaseController
{
    private readonly IVendorService _vendorService = vendorService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// [V-01] Retrieves a paginated list of vendors with optional search and filters.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "List of vendors retrieved successfully.", typeof(PaginatedList<VendorViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<PaginatedList<VendorViewModel>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? vendorType = null,
        [FromQuery] VendorStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var vendors = await _vendorService.GetPaginated(page, pageSize, search, vendorType, status, cancellationToken);
        return Ok(vendors);
    }

    /// <summary>
    /// [V-02] Retrieves single vendor details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [SwaggerResponse(200, "Vendor details retrieved successfully.", typeof(VendorViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Vendor not found.")]
    public async Task<ActionResult<VendorViewModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var vendor = await _vendorService.GetById(id, cancellationToken);
        return Ok(vendor);
    }

    /// <summary>
    /// [V-03] Creates a new vendor.
    /// </summary>
    [HttpPost]
    [SwaggerResponse(200, "Vendor created successfully.")]
    [SwaggerResponse(400, "Invalid request or duplicate email/phone/GST.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> Create([FromBody] VendorCreateRequest request, CancellationToken cancellationToken)
    {
        await _vendorService.Create(request, cancellationToken);
        return Ok(new { message = "Vendor created successfully." });
    }

    /// <summary>
    /// [V-04] Updates details of an existing vendor.
    /// </summary>
    [HttpPut("{id:guid}")]
    [SwaggerResponse(200, "Vendor updated successfully.")]
    [SwaggerResponse(400, "Invalid request or duplicate email/phone/GST.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Vendor not found.")]
    public async Task<IActionResult> Update(Guid id, [FromBody] VendorUpdateRequest request, CancellationToken cancellationToken)
    {
        await _vendorService.Update(id, request, cancellationToken);
        return Ok(new { message = "Vendor updated successfully." });
    }

    /// <summary>
    /// [V-05] Soft-deletes a vendor.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [SwaggerResponse(200, "Vendor deleted successfully.")]
    [SwaggerResponse(400, "Invalid Admin password.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Vendor not found.")]
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

        await _vendorService.Delete(id, cancellationToken);
        return Ok(new { message = "Vendor deleted successfully." });
    }
}
