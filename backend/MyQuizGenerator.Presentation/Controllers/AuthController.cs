using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Auth.Commands.ChangePassword;
using MyQuizGenerator.Application.Auth.Commands.ConfirmEmail;
using MyQuizGenerator.Application.Auth.Commands.ForgotPassword;
using MyQuizGenerator.Application.Auth.Commands.Login;
using MyQuizGenerator.Application.Auth.Commands.Logout;
using MyQuizGenerator.Application.Auth.Commands.RefreshToken;
using MyQuizGenerator.Application.Auth.Commands.Register;
using MyQuizGenerator.Application.Auth.Commands.ResetPassword;
using MyQuizGenerator.Application.Auth.Queries.GetCurrentUser;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Features.Auth.DTOs;

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
        return ApiCreated(response, "Registration successful. Please check your email to confirm your account.");
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

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var command = new LogoutCommand(request);
        await Mediator.Send(command);
        return ApiOk("Logout successful");
    }

    [Authorize]
    [HttpGet("me")]
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

    /// <summary>
    /// Confirms user's email address.
    /// </summary>
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        var command = new ConfirmEmailCommand(request);
        await Mediator.Send(command);
        return ApiOk("Email confirmed successfully. You can now login.");
    }

    /// <summary>
    /// Sends a password reset email to the user.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var command = new ForgotPasswordCommand(request);
        await Mediator.Send(command);
        return ApiOk("If the email exists in our system, you will receive a password reset link shortly.");
    }

    /// <summary>
    /// Resets the user's password with a valid token.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var command = new ResetPasswordCommand(request);
        await Mediator.Send(command);
        return ApiOk("Password has been reset successfully. You can now login with your new password.");
    }

    /// <summary>
    /// Changes the user's password (requires authentication).
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return ApiUnauthorized("Invalid token");
        }

        var command = new ChangePasswordCommand(userId, request);
        await Mediator.Send(command);
        return ApiOk("Password changed successfully");
    }
}
