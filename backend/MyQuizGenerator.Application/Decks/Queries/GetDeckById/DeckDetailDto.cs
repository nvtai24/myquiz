using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Application.Decks.Queries.GetUserDecks;

namespace MyQuizGenerator.Application.Decks.Queries.GetDeckById;

public class DeckDetailDto : DeckSummaryDto
{
    public List<QuestionDto> Questions { get; set; } = new();
}
