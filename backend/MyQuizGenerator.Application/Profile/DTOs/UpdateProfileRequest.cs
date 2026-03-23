using System.ComponentModel.DataAnnotations;

namespace MyQuizGenerator.Application.Profile.DTOs;

/// <summary>
/// Request DTO for updating user profile.
/// </summary>
public class UpdateProfileRequest
{
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string? FirstName { get; set; }

    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string? LastName { get; set; }

    [Url(ErrorMessage = "Invalid avatar URL format")]
    public string? AvatarUrl { get; set; }
}
