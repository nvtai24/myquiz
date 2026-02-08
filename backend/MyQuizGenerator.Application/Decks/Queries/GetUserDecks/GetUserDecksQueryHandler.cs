using MediatR;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;

namespace MyQuizGenerator.Application.Decks.Queries.GetUserDecks;

public class GetUserDecksQueryHandler : IRequestHandler<GetUserDecksQuery, List<DeckSummaryDto>>
{
    private readonly IDeckRepository _deckRepository;

    public GetUserDecksQueryHandler(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task<List<DeckSummaryDto>> Handle(GetUserDecksQuery request, CancellationToken cancellationToken)
    {
        var decks = await _deckRepository.GetDecksByUserIdAsync(request.UserId, cancellationToken);

        return decks.Select(d => new DeckSummaryDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            Visibility = d.Visibility,
            Tags = d.Tags,
            QuestionCount = d.Questions.Count,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        }).ToList();
    }
}
