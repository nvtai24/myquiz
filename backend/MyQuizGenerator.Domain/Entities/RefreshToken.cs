using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyQuizGenerator.Domain.Entities;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; }

    public string Token { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public DateTime CreationAt { get; set; }
    public DateTime ExpiryAt { get; set; }
    public bool Used { get; set; }
    public bool Invalidated { get; set; }

    public string UserId { get; set; } = string.Empty;
}
