using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Domain.Entities;

public class DeckInvitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeckId { get; set; }
    public virtual Deck Deck { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime SharedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiredAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public DateTime? AcceptedAt { get; set; }
    public DeckInvitationStatus Status { get; set; } = DeckInvitationStatus.Pending;
}
