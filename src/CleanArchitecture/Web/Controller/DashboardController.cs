using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
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
    /// [D-02] Get occupancy breakdown details for charts.
    /// </summary>
    [HttpGet("occupancy")]
    [SwaggerResponse(200, "Occupancy overview retrieved successfully.", typeof(OccupancyOverviewViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Dashboard permission required.")]
    public async Task<ActionResult<OccupancyOverviewViewModel>> GetOccupancy(
        [FromQuery] Guid? buildingId = null,
        CancellationToken cancellationToken = default)
    {
        var occupancy = await _dashboardService.GetOccupancy(buildingId, cancellationToken);
        return Ok(occupancy);
    }

    /// <summary>
    /// [D-03] Get expense breakdown grouped by category.
    /// </summary>
    [HttpGet("expense-breakdown")]
    [SwaggerResponse(200, "Expense breakdown retrieved successfully.", typeof(List<ExpenseBreakdownItemViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Dashboard permission required.")]
    public async Task<ActionResult<List<ExpenseBreakdownItemViewModel>>> GetExpenseBreakdown(
        [FromQuery] Guid? buildingId = null,
        CancellationToken cancellationToken = default)
    {
        var breakdown = await _dashboardService.GetExpenseBreakdown(buildingId, cancellationToken);
        return Ok(breakdown);
    }

    /// <summary>
    /// [D-04] Get recent payment activities.
    /// </summary>
    [HttpGet("recent-payments")]
    [SwaggerResponse(200, "Recent payments retrieved successfully.", typeof(List<DashboardRecentPaymentViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Dashboard permission required.")]
    public async Task<ActionResult<List<DashboardRecentPaymentViewModel>>> GetRecentPayments(
        [FromQuery] Guid? buildingId = null,
        CancellationToken cancellationToken = default)
    {
        var payments = await _dashboardService.GetRecentPayments(buildingId, cancellationToken);
        return Ok(payments);
    }

    /// <summary>
    /// [D-05] Get recent open/in-progress maintenance requests.
    /// </summary>
    [HttpGet("open-maintenance")]
    [SwaggerResponse(200, "Open maintenance requests retrieved successfully.", typeof(List<DashboardOpenMaintenanceViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Dashboard permission required.")]
    public async Task<ActionResult<List<DashboardOpenMaintenanceViewModel>>> GetOpenMaintenance(
        [FromQuery] Guid? buildingId = null,
        CancellationToken cancellationToken = default)
    {
        var maintenance = await _dashboardService.GetOpenMaintenance(buildingId, cancellationToken);
        return Ok(maintenance);
    }
}
