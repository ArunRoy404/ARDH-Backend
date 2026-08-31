using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Occupancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/occupancy-reports")]
public class OccupancyReportsController(IOccupancyReportService occupancyReportService) : BaseController
{
    private readonly IOccupancyReportService _occupancyReportService = occupancyReportService;

    /// <summary>
    /// [OR-01] Generates the per-apartment occupancy report (occupied/vacant days and rent figures)
    /// for a date range. The range is clamped to today - future occupancy is never calculated.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "Occupancy report generated successfully.", typeof(PaginatedList<OccupancyReportItemViewModel>))]
    [SwaggerResponse(400, "Invalid date range.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied.")]
    public async Task<ActionResult<PaginatedList<OccupancyReportItemViewModel>>> GetOccupancyReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] Guid? buildingId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _occupancyReportService.GetOccupancyReport(buildingId, fromDate, toDate, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [OR-02] Retrieves portfolio-wide occupancy totals (occupied/vacant days, expected rent,
    /// vacancy rent value, total potential rent) for a date range.
    /// </summary>
    [HttpGet("summary")]
    [SwaggerResponse(200, "Occupancy report summary retrieved successfully.", typeof(OccupancyReportSummaryViewModel))]
    [SwaggerResponse(400, "Invalid date range.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied.")]
    public async Task<ActionResult<OccupancyReportSummaryViewModel>> GetOccupancyReportSummary(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] Guid? buildingId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _occupancyReportService.GetOccupancyReportSummary(buildingId, fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [OR-03] Exports the occupancy report as an XLSX file.
    /// </summary>
    [HttpGet("export")]
    [SwaggerResponse(200, "XLSX occupancy report exported and downloaded successfully.", typeof(FileResult))]
    [SwaggerResponse(400, "Invalid date range.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied.")]
    public async Task<IActionResult> ExportOccupancyReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] Guid? buildingId = null,
        CancellationToken cancellationToken = default)
    {
        var bytes = await _occupancyReportService.ExportOccupancyReportToXlsx(buildingId, fromDate, toDate, cancellationToken);
        return File(bytes, CleanArchitecture.Application.Common.Utilities.XlsxHelper.ContentType, "occupancy_report.xlsx");
    }
}
