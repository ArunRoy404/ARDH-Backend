using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.AmcContract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/amc-contracts")]
public class AmcContractController(IAmcContractService amcContractService) : BaseController
{
    private readonly IAmcContractService _amcContractService = amcContractService;

    /// <summary>
    /// [AMC-01] Retrieves a paginated list of AMC contracts with optional search and filters.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "List of AMC contracts retrieved successfully.", typeof(PaginatedList<AmcContractViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<PaginatedList<AmcContractViewModel>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] AmcStatus? status = null,
        [FromQuery] AmcContractType? contractType = null,
        [FromQuery] Guid? vendorId = null,
        [FromQuery] Guid? equipmentId = null,
        CancellationToken cancellationToken = default)
    {
        var contracts = await _amcContractService.GetPaginated(page, pageSize, search, status, contractType, vendorId, equipmentId, cancellationToken);
        return Ok(contracts);
    }

    /// <summary>
    /// [AMC-03] Retrieves summary statistics for AMC contracts.
    /// </summary>
    [HttpGet("stats")]
    [SwaggerResponse(200, "AMC contract statistics retrieved successfully.", typeof(AmcContractStatsViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<AmcContractStatsViewModel>> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _amcContractService.GetStats(cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// [AMC-02] Retrieves single AMC contract details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [SwaggerResponse(200, "AMC contract details retrieved successfully.", typeof(AmcContractViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "AMC contract not found.")]
    public async Task<ActionResult<AmcContractViewModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var contract = await _amcContractService.GetById(id, cancellationToken);
        return Ok(contract);
    }

    /// <summary>
    /// [AMC-04] Creates a new AMC contract.
    /// </summary>
    [HttpPost]
    [SwaggerResponse(200, "AMC contract created successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> Create([FromBody] AmcContractCreateRequest request, CancellationToken cancellationToken)
    {
        await _amcContractService.Create(request, cancellationToken);
        return Ok(new { message = "AMC contract created successfully." });
    }

    /// <summary>
    /// [AMC-05] Updates details of an existing AMC contract.
    /// </summary>
    [HttpPut("{id:guid}")]
    [SwaggerResponse(200, "AMC contract updated successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "AMC contract not found.")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AmcContractUpdateRequest request, CancellationToken cancellationToken)
    {
        await _amcContractService.Update(id, request, cancellationToken);
        return Ok(new { message = "AMC contract updated successfully." });
    }

    /// <summary>
    /// [AMC-06] Soft-deletes an AMC contract.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [SwaggerResponse(200, "AMC contract deleted successfully.")]
    [SwaggerResponse(400, "Invalid admin password.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "AMC contract not found.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _amcContractService.Delete(id, cancellationToken);
        return Ok(new { message = "AMC contract deleted successfully." });
    }
}
