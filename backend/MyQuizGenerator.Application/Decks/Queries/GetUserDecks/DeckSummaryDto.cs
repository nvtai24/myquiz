using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.Queries.GetUserDecks;

public class DeckSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DeckVisibility Visibility { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public int QuestionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
