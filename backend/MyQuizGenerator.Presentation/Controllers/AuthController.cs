using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Auth.Commands.Login;
using MyQuizGenerator.Application.Auth.Commands.Register;
using MyQuizGenerator.Application.Auth.Queries.GetCurrentUser;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Features.Auth.DTOs;
using MyQuizGenerator.Domain.Constants;

namespace MyQuizGenerator.Presentation.Controllers;

/// <summary>
/// Handles authentication: register, login, token management.
/// </summary>
[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(request);

        var response = await Mediator.Send(command);
        return ApiCreated(response, "Registration successful");
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request);
        var response = await Mediator.Send(command);
        return ApiOk(response, "Login successful");
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Invalid token");
        }

        var query = new GetCurrentUserQuery(userId);
        var userInfo = await Mediator.Send(query);
        return ApiOk(userInfo, "User retrieved successfully");
    }

    [HttpGet("admin-only")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AdminOnly()
    {
        return ApiOk(new
        {
            Message = "Welcome, Admin! You have access to this protected resource.",
            AccessedAt = DateTime.UtcNow
        });
    }

    [HttpGet("user-area")]
    [Authorize(Policy = Policies.RequireUserRole)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult UserArea()
    {
        var firstName = User.FindFirst("firstName")?.Value ?? "User";
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        return ApiOk(new
        {
            Message = $"Hello {firstName}! Welcome to the user area.",
            Email = email,
            AccessedAt = DateTime.UtcNow
        });
    }
}
