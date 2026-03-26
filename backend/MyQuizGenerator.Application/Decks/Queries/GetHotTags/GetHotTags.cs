using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.Queries.GetHotTags;

public record GetHotTagsQuery(int Limit = 10) : IRequest<List<string>>;

public class GetHotTagsQueryHandler : IRequestHandler<GetHotTagsQuery, List<string>>
{
    private readonly IRepository<Guid, Deck> _deckRepository;

    public GetHotTagsQueryHandler(IRepository<Guid, Deck> deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task<List<string>> Handle(GetHotTagsQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 30);

        // Hot tags are computed from public + published decks only.
        var decks = await _deckRepository.GetQueryable()
            .AsNoTracking()
            .Where(d => d.Visibility == DeckVisibility.Public && d.Status == DeckStatus.Published)
            .Select(d => new HotTagDeckProjectionResponse
            {
                Tags = d.Tags,
                LastActiveAt = d.UpdatedAt ?? d.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Sort by frequency first, then by recency of the latest deck using that tag.
        var hotTags = decks
            .SelectMany(d => (d.Tags ?? Array.Empty<string>())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => new HotTagActivityResponse
                {
                    Tag = tag.Trim(),
                    LastActiveAt = d.LastActiveAt
                }))
            .GroupBy(x => x.Tag, StringComparer.OrdinalIgnoreCase)
            .Select(group => new HotTagAggregateResponse
            {
                Tag = group.First().Tag,
                Count = group.Count(),
                LatestAt = group.Max(x => x.LastActiveAt)
            })
            .OrderByDescending(x => x.Count)
            .ThenByDescending(x => x.LatestAt)
            .Take(limit)
            .Select(x => x.Tag)
            .ToList();

        return hotTags;
    }
}
