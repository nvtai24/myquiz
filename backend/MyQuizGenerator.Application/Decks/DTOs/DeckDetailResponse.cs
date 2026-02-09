namespace MyQuizGenerator.Application.Decks.DTOs;

public class DeckDetailResponse : DeckSummaryResponse
{
    public List<QuestionResponse> Questions { get; set; } = new();
    public List<DeckDocumentResponse> Documents { get; set; } = new();
}
