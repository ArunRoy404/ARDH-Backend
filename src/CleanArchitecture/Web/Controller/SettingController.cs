using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Models.Setting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/settings")]
public class SettingController(ISettingService settingService) : BaseController
{
    private readonly ISettingService _settingService = settingService;

    /// <summary>
    /// [S-01] Get current application settings.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "Application settings retrieved successfully.", typeof(SettingViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Settings not found.")]
    public async Task<ActionResult<SettingViewModel>> Get(CancellationToken cancellationToken)
    {
        var settings = await _settingService.Get(cancellationToken);
        return Ok(settings);
    }

    /// <summary>
    /// [S-02] Update general settings.
    /// </summary>
    [HttpPut]
    [SwaggerResponse(200, "Settings updated successfully.")]
    [SwaggerResponse(400, "Invalid request or incorrect password.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Settings not found.")]
    public async Task<IActionResult> Update([FromBody] SettingUpdateRequest request, CancellationToken cancellationToken)
    {
        await _settingService.Update(request, cancellationToken);
        return Ok(new { message = "Settings updated successfully." });
    }

    /// <summary>
    /// [S-03] Update system admin password.
    /// </summary>
    [HttpPut("password")]
    [SwaggerResponse(200, "Admin password updated successfully.")]
    [SwaggerResponse(400, "Invalid request or incorrect current password.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Settings not found.")]
    public async Task<IActionResult> UpdatePassword([FromBody] SettingUpdatePasswordRequest request, CancellationToken cancellationToken)
    {
        await _settingService.UpdatePassword(request, cancellationToken);
        return Ok(new { message = "Admin password updated successfully." });
    }
}
