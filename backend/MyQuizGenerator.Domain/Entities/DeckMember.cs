namespace MyQuizGenerator.Domain.Entities;

public class DeckMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeckId { get; set; }
    public virtual Deck Deck { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    // public DeckMemberRole Role { get; set; } = DeckMemberRole.Member;
}
