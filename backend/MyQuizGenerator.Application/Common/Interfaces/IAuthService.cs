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
    Task<UserInfo?> GetUserByIdAsync(string userId);

    /// <summary>
    /// Gets roles assigned to a user.
    /// </summary>
    Task<IList<string>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Checks if a user with the specified email exists.
    /// </summary>
    Task<bool> UserExistsAsync(string email);
}
