using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Features.Auth.DTOs;
using MyQuizGenerator.Domain.Constants;

namespace MyQuizGenerator.Infrastructure.Identity;

/// <summary>
/// Implementation of IAuthService using ASP.NET Identity.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<AuthService> _logger;


    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<(string UserId, string Email)> RegisterUserAsync(
        string email,
        string password,
        string? firstName,
        string? lastName)
    {
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            _logger.LogWarning("Failed to create user {Email}: {Errors}",
                email, string.Join(", ", errors));
            throw new Application.Common.Exceptions.ValidationException(errors);
        }

        await _userManager.AddToRoleAsync(user, Roles.User);

        return (user.Id, user.Email!);
    }

    public async Task<(bool Success, string? UserId, bool IsLockedOut)> CheckPasswordAsync(
        string email,
        string password)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return (false, null, false);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return (false, null, true);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            password,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return (false, null, true);
        }

        return (result.Succeeded, result.Succeeded ? user.Id : null, false);
    }

    public async Task<UserResponse?> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles.ToList()
        };
    }

    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return new List<string>();
        }

        return await _userManager.GetRolesAsync(user);
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user != null;
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Application.Common.Exceptions.NotFoundException("User", userId);
        }

        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<bool> ConfirmEmailAsync(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Email confirmation failed - user not found: {UserId}", userId);
            return false;
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (result.Succeeded)
        {
            _logger.LogInformation("Email confirmed successfully for user: {UserId}", userId);
            return true;
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        _logger.LogWarning("Email confirmation failed for user {UserId}: {Errors}", userId, errors);
        return false;
    }

    public async Task<bool> IsEmailConfirmedAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        return await _userManager.IsEmailConfirmedAsync(user);
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
            return null;
        }

        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Password reset failed - user not found: {Email}", email);
            return false;
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset successfully for user: {Email}", email);
            return true;
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        _logger.LogWarning("Password reset failed for {Email}: {Errors}", email, errors);
        return false;
    }

    public async Task<UserResponse?> GetUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles.ToList()
        };
    }

    public async Task ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Change password failed - user not found: {UserId}", userId);
            throw new Application.Common.Exceptions.NotFoundException("User", userId);
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            _logger.LogWarning("Change password failed for {UserId}: {Errors}", userId, string.Join(", ", errors));
            throw new Application.Common.Exceptions.ValidationException(errors);
        }

        _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
    }
}
