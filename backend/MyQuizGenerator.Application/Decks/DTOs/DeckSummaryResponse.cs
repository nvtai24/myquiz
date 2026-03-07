using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.DTOs;

public class DeckSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DeckVisibility Visibility { get; set; }
    public DeckStatus Status { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public int QuestionCount { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
