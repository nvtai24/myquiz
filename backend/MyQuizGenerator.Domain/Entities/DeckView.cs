namespace MyQuizGenerator.Domain.Entities;

public class DeckView
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeckId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
    public Deck Deck { get; set; } = null!;
}
