using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Shared.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Web.Controller;

[Authorize]
public class UserController(IUserService userService) : BaseController
{
    private readonly IUserService _userService = userService;

    /// <summary>
    /// Retrieves a list of all users.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [SwaggerResponse(200, "List of users retrieved successfully.", typeof(List<UserViewModel>))]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Admin role required.")]
    public async Task<ActionResult<List<UserViewModel>>> Get(CancellationToken cancellationToken)
    {
        var users = await _userService.Get(cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// Retrieves a user by their ID.
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
    /// Creates a new user. Only accessible by Admins.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerResponse(200, "User created successfully.")]
    [SwaggerResponse(400, "Invalid request or email already exists.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Admin role required.")]
    public async Task<IActionResult> Create([FromBody] UserCreateRequest request, CancellationToken cancellationToken)
    {
        await _userService.Create(request, cancellationToken);
        return Ok(new { message = "User created successfully." });
    }

    /// <summary>
    /// Updates the details of an existing user.
    /// </summary>
    [HttpPut]
    [SwaggerResponse(200, "User updated successfully.")]
    [SwaggerResponse(400, "Invalid request or email already exists.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(404, "User not found.")]
    public async Task<IActionResult> Update([FromBody] UserUpdateRequest request, CancellationToken cancellationToken)
    {
        await _userService.Update(request, cancellationToken);
        return Ok(new { message = "User updated successfully." });
    }

    /// <summary>
    /// Deletes a user by their ID. Only accessible by Admins.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerResponse(200, "User deleted successfully.")]
    [SwaggerResponse(401, "Unauthorized access.")]
    [SwaggerResponse(403, "Access denied. Admin role required.")]
    [SwaggerResponse(404, "User not found.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _userService.Delete(id, cancellationToken);
        return Ok(new { message = "User deleted successfully." });
    }
}
