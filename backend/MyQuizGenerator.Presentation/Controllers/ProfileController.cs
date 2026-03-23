using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Profile.Commands.UpdateProfile;
using MyQuizGenerator.Application.Profile.DTOs;
using MyQuizGenerator.Application.Profile.Queries.GetProfile;

namespace MyQuizGenerator.Presentation.Controllers;

/// <summary>
/// Handles user profile operations.
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class ProfileController : BaseApiController
{
    /// <summary>
    /// Gets the current user's profile including subscription info.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Invalid token");
        }

        var query = new GetProfileQuery(userId);
        var profile = await Mediator.Send(query);
        return ApiOk(profile, "Profile retrieved successfully");
    }

    /// <summary>
    /// Updates the current user's profile.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Invalid token");
        }

        var command = new UpdateProfileCommand(userId, request);
        var updatedUser = await Mediator.Send(command);
        return ApiOk(updatedUser, "Profile updated successfully");
    }
}
