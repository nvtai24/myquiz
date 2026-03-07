using MediatR;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;

namespace MyQuizGenerator.Application.Decks.Queries.GetAttemptedDecks;

public record GetAttemptedDecksQuery(string UserId) : IRequest<List<DeckSummaryResponse>>;

public class GetAttemptedDecksQueryHandler : IRequestHandler<GetAttemptedDecksQuery, List<DeckSummaryResponse>>
{
    private readonly IDeckRepository _deckRepository;

    public GetAttemptedDecksQueryHandler(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task<List<DeckSummaryResponse>> Handle(GetAttemptedDecksQuery request, CancellationToken cancellationToken)
    {
        var decks = await _deckRepository.GetAttemptedDecksAsync(request.UserId, cancellationToken);

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
