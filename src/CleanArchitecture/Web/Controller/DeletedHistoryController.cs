using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.DeletedHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/deleted-history")]
public class DeletedHistoryController(IDeletedHistoryService deletedHistoryService) : BaseController
{
    private readonly IDeletedHistoryService _deletedHistoryService = deletedHistoryService;

    /// <summary>
    /// [DH-01] List all soft-deleted records with pagination, search, and filters.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "List of soft-deleted records retrieved successfully.", typeof(PaginatedList<DeletedHistoryViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<PaginatedList<DeletedHistoryViewModel>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery(Name = "entity_type")] string? entityType = null,
        [FromQuery(Name = "start_date")] DateTime? startDate = null,
        [FromQuery(Name = "end_date")] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _deletedHistoryService.GetPaginated(page, pageSize, search, entityType, startDate, endDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [DH-04] Get full details of a deleted record by history ID.
    /// </summary>
    [HttpGet("{id}")]
    [SwaggerResponse(200, "Deleted history details retrieved successfully.", typeof(DeletedHistoryDetailsViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Deleted history record not found.")]
    public async Task<ActionResult<DeletedHistoryDetailsViewModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deletedHistoryService.GetById(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [DH-02] Restore a deleted record by history ID.
    /// </summary>
    [HttpPost("{id}/restore")]
    [SwaggerResponse(200, "Record restored successfully.")]
    [SwaggerResponse(400, "Record already restored or invalid entity state.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Deleted history record not found.")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        await _deletedHistoryService.Restore(id, cancellationToken);
        return Ok(new { message = "Record restored successfully." });
    }

    /// <summary>
    /// [DH-03] Permanently delete a deleted history record by history ID.
    /// </summary>
    [HttpDelete("{id}")]
    [SwaggerResponse(200, "Deleted history record and its underlying entity permanently deleted successfully.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Deleted history record not found.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _deletedHistoryService.DeletePermanently(id, cancellationToken);
        return Ok(new { message = "Deleted history record permanently deleted successfully." });
    }
}
