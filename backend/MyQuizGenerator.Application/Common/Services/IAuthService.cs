using MyQuizGenerator.Application.Auth.DTOs;
using MyQuizGenerator.Application.Admin.DTOs;

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

    /// <summary>
    /// Generates a password reset token for the user.
    /// </summary>
    Task<string?> GeneratePasswordResetTokenAsync(string email);

    /// <summary>
    /// Resets the user's password with the provided token.
    /// </summary>
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);

    /// <summary>
    /// Gets user by email.
    /// </summary>
    Task<UserResponse?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Changes the user's password. Throws ValidationException on failure.
    /// </summary>
    Task ChangePasswordAsync(string userId, string currentPassword, string newPassword);

    /// <summary>
    /// Authenticates or registers a user via Google OAuth using ID Token.
    /// Returns user info if successful.
    /// </summary>
    Task<(string UserId, string Email, string? FirstName, string? LastName, bool IsNewUser)>
        GoogleLoginAsync(string idToken);

    /// <summary>
    /// Gets a paginated list of users with optional search filter.
    /// </summary>
    Task<(List<AdminUserResponse> Users, int TotalCount)> GetUsersAsync(int page, int pageSize, string? search);

    /// <summary>
    /// Bans or unbans a user by setting lockout.
    /// </summary>
    Task BanUserAsync(string userId, bool isBanned);

    /// <summary>
    /// Assigns a role to a user, replacing existing roles.
    /// </summary>
    Task AssignRoleAsync(string userId, string role);
}

