namespace MyQuizGenerator.Application.Profile.DTOs;

/// <summary>
/// Response DTO for user profile.
/// </summary>
public class ProfileResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? CurrentPlan { get; set; }
}
