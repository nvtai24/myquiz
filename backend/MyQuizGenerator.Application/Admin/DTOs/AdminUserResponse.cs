namespace MyQuizGenerator.Application.Admin.DTOs;

/// <summary>
/// User details DTO for admin management.
/// </summary>
public class AdminUserResponse
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool EmailConfirmed { get; set; }
    public bool IsBanned { get; set; }
    public DateTime CreatedAt { get; set; }
}
