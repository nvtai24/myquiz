using MediatR;
using MyQuizGenerator.Application.Common.DTOs;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;

namespace MyQuizGenerator.Application.Decks.Queries.GetDrafts;

public record GetDraftsQuery(string UserId, int Page = 1, int Size = 10) : IRequest<PagedResult<DeckSummaryResponse>>;

public class GetDraftsQueryHandler : IRequestHandler<GetDraftsQuery, PagedResult<DeckSummaryResponse>>
{
    private readonly IDeckRepository _deckRepository;

    public GetDraftsQueryHandler(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task<PagedResult<DeckSummaryResponse>> Handle(GetDraftsQuery request, CancellationToken cancellationToken)
    {
        var (decks, totalCount) = await _deckRepository.GetMyDraftsAsync(request.UserId, request.Page, request.Size, cancellationToken);

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
