using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Domain.Enums;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Web.Controller;

[Authorize(Roles = "SuperAdmin,Admin")]
[Route("api/users")]
public class UserController(IUserService userService) : BaseController
{
    private readonly IUserService _userService = userService;

    /// <summary>
    /// [U-01] Retrieves a paginated, filtered list of all users.
    /// </summary>
    [HttpGet]
    [SwaggerResponse(200, "List of users retrieved successfully.", typeof(PaginatedList<UserViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. SuperAdmin or Admin role required.")]
    public async Task<ActionResult<PaginatedList<UserViewModel>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] UserRole? role = null,
        [FromQuery(Name = "is_active")] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var users = await _userService.GetPaginated(page, pageSize, search, role, isActive, cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// [U-02] Retrieves a user by their ID.
    /// </summary>
    [HttpGet("{id}")]
    [SwaggerResponse(200, "User details retrieved successfully.", typeof(UserViewModel))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "User not found.")]
    public async Task<ActionResult<UserViewModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetById(id, cancellationToken);
        return Ok(user);
    }

    /// <summary>
    /// [U-03] Creates a new user.
    /// </summary>
    [HttpPost]
    [SwaggerResponse(200, "User created successfully.")]
    [SwaggerResponse(400, "Invalid request or email already exists.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    public async Task<IActionResult> Create([FromBody] UserCreateRequest request, CancellationToken cancellationToken)
    {
        await _userService.Create(request, cancellationToken);
        return Ok(new { message = "User created successfully." });
    }

    /// <summary>
    /// [U-04] Updates the details of an existing user.
    /// </summary>
    [HttpPut("{id}")]
    [SwaggerResponse(200, "User updated successfully.")]
    [SwaggerResponse(400, "Invalid request or email already exists.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "User not found.")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserUpdateRequest request, CancellationToken cancellationToken)
    {
        request.Id = id;
        await _userService.Update(request, cancellationToken);
        return Ok(new { message = "User updated successfully." });
    }

    /// <summary>
    /// [U-05] Soft-deletes a user by their ID.
    /// </summary>
    [HttpDelete("{id}")]
    [SwaggerResponse(200, "User deleted successfully.")]
    [SwaggerResponse(400, "Invalid SuperAdmin password.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "User not found.")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(password))
        {
            if (HttpContext.Request.Headers.TryGetValue("X-SuperAdmin-Password", out var headerPassword))
            {
                password = headerPassword.ToString();
            }
        }

        if (password != "123456")
        {
            return BadRequest(new { message = "Invalid SuperAdmin password." });
        }

        await _userService.Delete(id, cancellationToken);
        return Ok(new { message = "User deleted successfully." });
    }

    /// <summary>
    /// [U-06] Toggles active/inactive status of a user.
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [SwaggerResponse(200, "User status toggled successfully.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "User not found.")]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        await _userService.ToggleStatus(id, cancellationToken);
        return Ok(new { message = "User status toggled successfully." });
    }
}
