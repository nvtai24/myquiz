using MyQuizGenerator.Application.Features.Auth.DTOs;

namespace MyQuizGenerator.Application.Common.Interfaces;

/// <summary>
/// Authentication service interface for user management.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user with the specified credentials.
    /// </summary>
    Task<(string UserId, string Email)> RegisterUserAsync(
        string email,
        string password,
        string? firstName,
        string? lastName);

    /// <summary>
    /// Validates user credentials and returns login result.
    /// </summary>
    Task<(bool Success, string? UserId, bool IsLockedOut)> CheckPasswordAsync(
        string email,
        string password);

    /// <summary>
    /// Gets user information by ID.
    /// </summary>
    Task<UserResponse?> GetUserByIdAsync(string userId);

    /// <summary>
    /// Gets roles assigned to a user.
    /// </summary>
    Task<IList<string>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Checks if a user with the specified email exists.
    /// </summary>
    Task<bool> UserExistsAsync(string email);

    /// <summary>
    /// Generates an email confirmation token for the user.
    /// </summary>
    Task<string> GenerateEmailConfirmationTokenAsync(string userId);

    /// <summary>
    /// Confirms the user's email with the provided token.
    /// </summary>
    Task<bool> ConfirmEmailAsync(string userId, string token);

    /// <summary>
    /// Checks if the user's email is confirmed.
    /// </summary>
    Task<bool> IsEmailConfirmedAsync(string userId);
}
