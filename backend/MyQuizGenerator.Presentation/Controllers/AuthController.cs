using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Auth.Commands.Login;
using MyQuizGenerator.Application.Auth.Commands.RefreshToken;
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
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(request);

        var response = await Mediator.Send(command);
        return ApiCreated(response, "Registration successful");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request);
        var response = await Mediator.Send(command);
        return ApiOk(response, "Login successful");
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand(request);
        var response = await Mediator.Send(command);
        return ApiOk(response, "Token refreshed successfully");
    }

    [HttpGet("me")]
    [Authorize]
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


}
