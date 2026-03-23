using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Files.DTOs;
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
    private readonly IFileService _fileService;

    public ProfileController(IFileService fileService)
    {
        _fileService = fileService;
    }

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
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileRequest request, IFormFile? avatar)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Invalid token");
        }

        string? avatarUrl = null;
        if (avatar != null && avatar.Length > 0)
        {
            using var stream = avatar.OpenReadStream();
            var fileRequest = new FileUploadRequest(stream, avatar.FileName, avatar.ContentType);
            avatarUrl = await _fileService.UploadFileAsync(fileRequest);
        }

        var command = new UpdateProfileCommand(userId, request, avatarUrl);
        var updatedUser = await Mediator.Send(command);
        return ApiOk(updatedUser, "Profile updated successfully");
    }
}
