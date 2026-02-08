using MediatR;

namespace MyQuizGenerator.Application.Decks.Queries.GetDeckById;

public record GetDeckByIdQuery(Guid Id) : IRequest<DeckDetailDto>;
