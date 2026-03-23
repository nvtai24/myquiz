using MediatR;
using MyQuizGenerator.Application.Common.DTOs;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;

namespace MyQuizGenerator.Application.Decks.Queries.GetAttemptedDecks;

public record GetAttemptedDecksQuery(string UserId, int Page = 1, int Size = 10) : IRequest<PagedResult<DeckSummaryResponse>>;

public class GetAttemptedDecksQueryHandler : IRequestHandler<GetAttemptedDecksQuery, PagedResult<DeckSummaryResponse>>
{
    private readonly IDeckRepository _deckRepository;

    public GetAttemptedDecksQueryHandler(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task<PagedResult<DeckSummaryResponse>> Handle(GetAttemptedDecksQuery request, CancellationToken cancellationToken)
    {
        var (decks, totalCount) = await _deckRepository.GetAttemptedDecksAsync(request.UserId, request.Page, request.Size, cancellationToken);

        var items = decks.Select(d => new DeckSummaryResponse
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            Visibility = d.Visibility,
            Status = d.Status,
            Tags = d.Tags ?? [],
            QuestionCount = d.Questions.Count,
            ThumbnailUrl = d.ThumbnailUrl ?? string.Empty,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt,
            AverageRating = d.DeckRatings.Count != 0 ? Math.Round(d.DeckRatings.Average(r => r.Rating), 1) : 0,
            TotalRatings = d.DeckRatings.Count
        }).ToList();

        return new PagedResult<DeckSummaryResponse>(items, request.Page, request.Size, totalCount);
    }
}
