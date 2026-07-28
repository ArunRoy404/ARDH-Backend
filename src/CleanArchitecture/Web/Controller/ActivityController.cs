using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/activities")]
public class ActivityController(IActivityService activityService) : BaseController
{
    private readonly IActivityService _activityService = activityService;

    /// <summary>
    /// [ACT-01] List recent activities with pagination and optional building filtering.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "Recent activities retrieved successfully.", typeof(PaginatedList<ActivityViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Dashboard permission required.")]
    public async Task<ActionResult<PaginatedList<ActivityViewModel>>> GetActivities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? buildingId = null,
        CancellationToken cancellationToken = default)
    {
        var activities = await _activityService.GetPaginated(page, pageSize, buildingId, cancellationToken);
        return Ok(activities);
    }
}
