using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Income;
using CleanArchitecture.Shared.Models.Expenses;
using CleanArchitecture.Shared.Models.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/reports")]
public class ReportsController(IReportService reportService) : BaseController
{
    private readonly IReportService _reportService = reportService;

    /// <summary>
    /// [R-01] Generates income report with pagination and filters.
    /// </summary>
    [HttpGet("income")]
    [SwaggerResponse(200, "Income report generated successfully.", typeof(PaginatedList<IncomeRecordViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied.")]
    public async Task<ActionResult<PaginatedList<IncomeRecordViewModel>>> GetIncomeReport(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetIncomeReport(buildingId, startDate, endDate, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [R-02] Generates expense report with pagination and filters.
    /// </summary>
    [HttpGet("expenses")]
    [SwaggerResponse(200, "Expense report generated successfully.", typeof(PaginatedList<ExpenseRecordViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied.")]
    public async Task<ActionResult<PaginatedList<ExpenseRecordViewModel>>> GetExpenseReport(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetExpenseReport(buildingId, startDate, endDate, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [R-03] Retrieves total incomes, total expenses, and net cash flow stats.
    /// </summary>
    [HttpGet("stats")]
    [SwaggerResponse(200, "Report stats retrieved successfully.", typeof(ReportStatsViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied.")]
    public async Task<ActionResult<ReportStatsViewModel>> GetReportStats(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetReportStats(buildingId, startDate, endDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// [R-04] Exports income, expense, or combined report as an XLSX file.
    /// </summary>
    [HttpGet("export")]
    [SwaggerResponse(200, "XLSX report exported and downloaded successfully.", typeof(FileResult))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied.")]
    public async Task<IActionResult> ExportReport(
        [FromQuery] string type = "combined",
        [FromQuery] Guid? buildingId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var bytes = await _reportService.ExportReportToXlsx(type, buildingId, startDate, endDate, cancellationToken);
        var fileName = $"{type.Trim().ToLower()}_report.xlsx";
        return File(bytes, CleanArchitecture.Application.Common.Utilities.XlsxHelper.ContentType, fileName);
    }
}
