namespace MyQuizGenerator.Domain.Entities;

public class SavedDeck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeckId { get; set; }
    public virtual Deck Deck { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
