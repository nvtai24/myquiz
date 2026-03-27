using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Auth.Commands.ChangePassword;
using MyQuizGenerator.Application.Auth.Commands.ConfirmEmail;
using MyQuizGenerator.Application.Auth.Commands.ForgotPassword;
using MyQuizGenerator.Application.Auth.Commands.GoogleLogin;
using MyQuizGenerator.Application.Auth.Commands.Login;
using MyQuizGenerator.Application.Auth.Commands.Logout;
using MyQuizGenerator.Application.Auth.Commands.RefreshToken;
using MyQuizGenerator.Application.Auth.Commands.Register;
using MyQuizGenerator.Application.Auth.Commands.ResetPassword;
using MyQuizGenerator.Application.Auth.Queries.GetCurrentUser;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Auth.DTOs;
using System.Net;

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
        SetCookies(response.AccessToken, response.RefreshToken, response.ExpiresAt);

        return ApiOk(response, "Login successful");
    }

    /// <summary>
    /// Authenticates user via Google OAuth.
    /// </summary>
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var command = new GoogleLoginCommand(request);
        var response = await Mediator.Send(command);
        SetCookies(response.AccessToken, response.RefreshToken, response.ExpiresAt);
        return ApiOk(response, "Google login successful");
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request)
    {
        // Priority: read from cookie (HttpOnly), fallback to body for backward compatibility
        var refreshToken = Request.Cookies["refresh_token"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            refreshToken = request?.RefreshToken;
        }

        if (string.IsNullOrEmpty(refreshToken))
        {
            return ApiBadRequest("Refresh token is required");
        }

        var command = new RefreshTokenCommand(new RefreshTokenRequest { RefreshToken = refreshToken });
        var response = await Mediator.Send(command);
        SetCookies(response.AccessToken, response.RefreshToken, response.ExpiresAt);
        return ApiOk(response, "Token refreshed successfully");
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
    {
        // Read tokens from cookies (primary) or body (backward compatibility)
        var refreshToken = Request.Cookies["refresh_token"] ?? request?.RefreshToken;
        var accessToken = Request.Cookies["access_token"] ?? request?.AccessToken;

        var logoutRequest = new LogoutRequest
        {
            RefreshToken = refreshToken,
            AccessToken = accessToken
        };

        var command = new LogoutCommand(logoutRequest);
        await Mediator.Send(command);

        // Clear cookies (must match the Path used when setting)
        Response.Cookies.Delete("access_token", new CookieOptions { Path = "/" });
        Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/auth" });

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


    private void SetCookies(string accessToken, string refreshToken, DateTime expiresAt)
    {
        Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiresAt,
            Path = "/"
        });

        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiresAt.AddDays(7),
            Path = "/api/auth" // only send to Auth endpoints (refresh-token, logout)
        });
    }
}
