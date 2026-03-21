using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Admin.Commands.AssignRole;
using MyQuizGenerator.Application.Admin.Commands.BanUser;
using MyQuizGenerator.Application.Admin.DTOs;
using MyQuizGenerator.Application.Admin.Queries.GetUsers;

namespace MyQuizGenerator.Presentation.Controllers;

/// <summary>
/// Admin endpoints for user management.
/// </summary>
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : BaseApiController
{
    /// <summary>
    /// Gets a paginated list of users with optional search and filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isBanned = null)
    {
        var query = new GetUsersQuery(page, pageSize, search, role, isBanned);
        var (users, totalCount) = await Mediator.Send(query);
        return ApiPaged(users, page, pageSize, totalCount);
    }

    /// <summary>
    /// Bans or unbans a user.
    /// </summary>
    [HttpPut("{userId}/ban")]
    public async Task<IActionResult> BanUser(string userId, [FromBody] BanUserRequest request)
    {
        var command = new BanUserCommand(userId, request.IsBanned);
        await Mediator.Send(command);

        var message = request.IsBanned ? "User banned successfully" : "User unbanned successfully";
        return ApiOk(message);
    }

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    [HttpPut("{userId}/role")]
    public async Task<IActionResult> AssignRole(string userId, [FromBody] AssignRoleRequest request)
    {
        var command = new AssignRoleCommand(userId, request.Role);
        await Mediator.Send(command);
        return ApiOk($"Role '{request.Role}' assigned successfully");
    }
}
