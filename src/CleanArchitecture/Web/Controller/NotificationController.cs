using System;
using System.Threading;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanArchitecture.Web.Controller;

[Authorize]
[Route("api/notifications")]
public class NotificationController(INotificationService notificationService) : BaseController
{
    private readonly INotificationService _notificationService = notificationService;

    /// <summary>
    /// [N-01] List all notifications with pagination and filtering by type/is_read.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "Notifications retrieved successfully.", typeof(PaginatedList<NotificationViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<PaginatedList<NotificationViewModel>>> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? type = null,
        [FromQuery] bool? is_read = null,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationService.GetNotifications(page, pageSize, type, is_read, cancellationToken);
        return Ok(notifications);
    }

    /// <summary>
    /// [N-02] Get unread notification count, and total count for the current user.
    /// </summary>
    [HttpGet("count")]
    [SwaggerResponse(200, "Notification counts retrieved successfully.", typeof(NotificationCountViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<ActionResult<NotificationCountViewModel>> GetCount(CancellationToken cancellationToken)
    {
        var count = await _notificationService.GetCount(cancellationToken);
        return Ok(count);
    }

    /// <summary>
    /// [N-03] Mark a single notification as read for the current user.
    /// </summary>
    [HttpPatch("{id}/read")]
    [SwaggerResponse(200, "Notification marked as read successfully.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Notification not found.")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await _notificationService.MarkAsRead(id, cancellationToken);
        return Ok(new { message = "Notification marked as read successfully." });
    }

    /// <summary>
    /// [N-04] Mark all notifications as read for the current user.
    /// </summary>
    [HttpPatch("read-all")]
    [SwaggerResponse(200, "All notifications marked as read successfully.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllAsRead(cancellationToken);
        return Ok(new { message = "All notifications marked as read successfully." });
    }

    /// <summary>
    /// [N-05] Create a notification (system-generated).
    /// </summary>
    [HttpPost]
    [SwaggerResponse(200, "Notification created successfully.")]
    [SwaggerResponse(400, "Invalid request payload.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> Create([FromBody] NotificationCreateRequest request, CancellationToken cancellationToken)
    {
        await _notificationService.CreateNotification(request, cancellationToken);
        return Ok(new { message = "Notification created successfully." });
    }

    /// <summary>
    /// [N-06] Permanently Delete a single notification for the current user.
    /// </summary>
    [HttpDelete("{id}")]
    [SwaggerResponse(200, "Notification deleted successfully.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "Notification not found.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _notificationService.Delete(id, cancellationToken);
        return Ok(new { message = "Notification deleted successfully." });
    }

    /// <summary>
    /// [N-07] Permanently delete all notifications for the current user.
    /// </summary>
    [HttpDelete("clear-all")]
    [SwaggerResponse(200, "All notifications cleared successfully.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> ClearAll(CancellationToken cancellationToken)
    {
        await _notificationService.ClearAll(cancellationToken);
        return Ok(new { message = "All notifications cleared successfully." });
    }
}
