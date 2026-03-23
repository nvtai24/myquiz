namespace MyQuizGenerator.Application.Decks.DTOs;

public class DeckDetailResponse : DeckSummaryResponse
{
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public List<QuestionResponse> Questions { get; set; } = new();
    public string? DocumentUrl { get; set; }
}
