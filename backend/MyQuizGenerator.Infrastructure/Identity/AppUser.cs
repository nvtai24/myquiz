using Microsoft.AspNetCore.Identity;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Infrastructure.Identity;

/// <summary>
/// Application user entity for ASP.NET Identity.
/// </summary>
public class AppUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
