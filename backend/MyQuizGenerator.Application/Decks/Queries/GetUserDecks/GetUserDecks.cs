using MediatR;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;

namespace MyQuizGenerator.Application.Decks.Queries.GetUserDecks;

public record GetUserDecksQuery(string UserId) : IRequest<List<DeckSummaryResponse>>;

public class GetUserDecksQueryHandler : IRequestHandler<GetUserDecksQuery, List<DeckSummaryResponse>>
{
    private readonly IDeckRepository _deckRepository;

    public GetUserDecksQueryHandler(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task<List<DeckSummaryResponse>> Handle(GetUserDecksQuery request, CancellationToken cancellationToken)
    {
        var decks = await _deckRepository.GetDecksByUserIdAsync(request.UserId, cancellationToken);

        return decks.Select(d => new DeckSummaryResponse
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            Visibility = d.Visibility,
            Tags = d.Tags,
            QuestionCount = d.Questions.Count,
            ThumbnailUrl = d.ThumbnailUrl,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        }).ToList();
    }
}
