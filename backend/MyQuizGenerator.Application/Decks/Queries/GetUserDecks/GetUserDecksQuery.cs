using MediatR;

namespace MyQuizGenerator.Application.Decks.Queries.GetUserDecks;

public record GetUserDecksQuery(string UserId) : IRequest<List<DeckSummaryDto>>;
