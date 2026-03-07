namespace MyQuizGenerator.Application.Decks.DTOs;

public class SharedDeckResponse : DeckSummaryResponse
{
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
}
