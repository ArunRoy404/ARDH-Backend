using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/dashboard")]
public class DashboardController(IDashboardService dashboardService) : BaseController
{
    private readonly IDashboardService _dashboardService = dashboardService;

    /// <summary>
    /// [D-01] Get dashboard general statistics and overview counters.
    /// </summary>
    [HttpGet("stats")]
    [SwaggerResponse(200, "Dashboard stats retrieved successfully.", typeof(DashboardStatsViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Dashboard permission required.")]
    public async Task<ActionResult<DashboardStatsViewModel>> GetStats(
        [FromQuery] Guid? buildingId = null,
        CancellationToken cancellationToken = default)
    {
        var stats = await _dashboardService.GetStats(buildingId, cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// [D-02] Get occupancy breakdown details for charts. Without startDate/endDate,
    /// returns the current real-time snapshot. With a range, Occupied/Reserved are
    /// re-derived from lease overlap with that range instead of "right now".
    /// </summary>
    [HttpGet("occupancy")]
    [SwaggerResponse(200, "Occupancy overview retrieved successfully.", typeof(OccupancyOverviewViewModel))]
    [SwaggerResponse(400, "startDate is later than endDate.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Dashboard permission required.")]
    public async Task<ActionResult<OccupancyOverviewViewModel>> GetOccupancy(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var occupancy = await _dashboardService.GetOccupancy(buildingId, startDate, endDate, cancellationToken);
        return Ok(occupancy);
    }

    /// <summary>
    /// [D-03] Get expense breakdown grouped by category. Without startDate/endDate,
    /// returns the all-time total; with a range, only expenses dated within it.
    /// </summary>
    [HttpGet("expense-breakdown")]
    [SwaggerResponse(200, "Expense breakdown retrieved successfully.", typeof(List<ExpenseBreakdownItemViewModel>))]
    [SwaggerResponse(400, "startDate is later than endDate.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Dashboard permission required.")]
    public async Task<ActionResult<List<ExpenseBreakdownItemViewModel>>> GetExpenseBreakdown(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var breakdown = await _dashboardService.GetExpenseBreakdown(buildingId, startDate, endDate, cancellationToken);
        return Ok(breakdown);
    }

    /// <summary>
    /// [D-04] Get recent payment activities.
    /// </summary>
    [HttpGet("recent-payments")]
    [SwaggerResponse(200, "Recent payments retrieved successfully.", typeof(PaginatedList<DashboardRecentPaymentViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Dashboard permission required.")]
    public async Task<ActionResult<PaginatedList<DashboardRecentPaymentViewModel>>> GetRecentPayments(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var payments = await _dashboardService.GetRecentPayments(buildingId, page, pageSize, cancellationToken);
        return Ok(payments);
    }

    /// <summary>
    /// [D-05] Get recent open/in-progress maintenance requests.
    /// </summary>
    [HttpGet("open-maintenance")]
    [SwaggerResponse(200, "Open maintenance requests retrieved successfully.", typeof(PaginatedList<DashboardOpenMaintenanceViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Dashboard permission required.")]
    public async Task<ActionResult<PaginatedList<DashboardOpenMaintenanceViewModel>>> GetOpenMaintenance(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var maintenance = await _dashboardService.GetOpenMaintenance(buildingId, page, pageSize, cancellationToken);
        return Ok(maintenance);
    }

    /// <summary>
    /// [D-06] Get total paid income for a date range. Without startDate/endDate,
    /// returns the all-time total.
    /// </summary>
    [HttpGet("income-stat")]
    [SwaggerResponse(200, "Income stat retrieved successfully.", typeof(DashboardAmountStatViewModel))]
    [SwaggerResponse(400, "startDate is later than endDate.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Dashboard permission required.")]
    public async Task<ActionResult<DashboardAmountStatViewModel>> GetIncomeStat(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var stat = await _dashboardService.GetIncomeStat(buildingId, startDate, endDate, cancellationToken);
        return Ok(stat);
    }

    /// <summary>
    /// [D-07] Get total paid expense for a date range. Without startDate/endDate,
    /// returns the all-time total.
    /// </summary>
    [HttpGet("expense-stat")]
    [SwaggerResponse(200, "Expense stat retrieved successfully.", typeof(DashboardAmountStatViewModel))]
    [SwaggerResponse(400, "startDate is later than endDate.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Dashboard permission required.")]
    public async Task<ActionResult<DashboardAmountStatViewModel>> GetExpenseStat(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var stat = await _dashboardService.GetExpenseStat(buildingId, startDate, endDate, cancellationToken);
        return Ok(stat);
    }
}
