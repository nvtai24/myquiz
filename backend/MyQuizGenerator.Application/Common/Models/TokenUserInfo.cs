namespace MyQuizGenerator.Application.Common.Models;

/// <summary>
/// User information needed for token generation.
/// </summary>
public record TokenUserInfo(
    string Id,
    string Email,
    string? FirstName,
    string? LastName
);
