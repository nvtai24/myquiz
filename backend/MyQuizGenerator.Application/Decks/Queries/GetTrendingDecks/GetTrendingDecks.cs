using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.Queries.GetTrendingDecks;

public record GetTrendingDecksQuery(int Limit = 12) : IRequest<List<SharedDeckResponse>>;

public class GetTrendingDecksQueryHandler : IRequestHandler<GetTrendingDecksQuery, List<SharedDeckResponse>>
{
    private readonly IRepository<Guid, Deck> _deckRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserService _userService;

    public GetTrendingDecksQueryHandler(
        IRepository<Guid, Deck> deckRepository,
        ICurrentUserService currentUserService,
        IUserService userService)
    {
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
        _userService = userService;
    }

    public async Task<List<SharedDeckResponse>> Handle(GetTrendingDecksQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedException();
        var limit = Math.Clamp(request.Limit, 1, 30);
        var now = DateTime.UtcNow;
        var recentSince = now.AddDays(-30);

        // Pull metrics needed for trend scoring in one query.
        var candidates = await _deckRepository.GetQueryable()
            .AsNoTracking()
            .Where(d => d.OwnerId != userId
                        && d.Visibility == DeckVisibility.Public
                        && d.Status == DeckStatus.Published)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Description,
                d.Visibility,
                d.Status,
                d.Tags,
                QuestionCount = d.Questions.Count,
                ThumbnailUrl = d.ThumbnailUrl,
                d.CreatedAt,
                d.UpdatedAt,
                d.OwnerId,
                TotalRatings = d.DeckRatings.Count,
                AverageRating = d.DeckRatings.Count == 0 ? 0 : d.DeckRatings.Average(r => (double)r.Rating),
                ViewCount = d.DeckViews.Count,
                RecentViewCount = d.DeckViews.Count(v => v.ViewedAt >= recentSince),
                SaveCount = d.SavedByUsers.Count,
                RecentSaveCount = d.SavedByUsers.Count(s => s.SavedAt >= recentSince),
                AttemptCount = d.QuizAttempts.Count,
                RecentAttemptCount = d.QuizAttempts.Count(a => a.StartedAt >= recentSince)
            })
            .ToListAsync(cancellationToken);

        // Reuse a small set of hot tags to reward decks matching current tag momentum.
        var hotTagSet = candidates
            .SelectMany(d => (d.Tags ?? Array.Empty<string>())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => new
                {
                    Tag = tag.Trim(),
                    LastActiveAt = d.UpdatedAt ?? d.CreatedAt
                }))
            .GroupBy(x => x.Tag, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Tag = group.First().Tag,
                Count = group.Count(),
                LatestAt = group.Max(x => x.LastActiveAt)
            })
            .OrderByDescending(x => x.Count)
            .ThenByDescending(x => x.LatestAt)
            .Take(10)
            .Select(x => x.Tag)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Trend score prioritizes recent engagement, then global popularity and freshness.
        var rankedDecks = candidates
            .Select(d =>
            {
                var freshestAt = d.UpdatedAt ?? d.CreatedAt;
                var hotTagBonus = (d.Tags ?? Array.Empty<string>())
                    .Count(tag => !string.IsNullOrWhiteSpace(tag) && hotTagSet.Contains(tag.Trim())) * 4.0;

                var freshnessScore = freshestAt >= now.AddDays(-7)
                    ? 10
                    : freshestAt >= now.AddDays(-30)
                        ? 5
                        : freshestAt >= now.AddDays(-90)
                            ? 2
                            : 0;

                var trendScore =
                    (d.RecentViewCount * 8.0) +
                    (d.RecentSaveCount * 6.0) +
                    (d.RecentAttemptCount * 4.0) +
                    (d.ViewCount * 1.5) +
                    (d.SaveCount * 2.0) +
                    (d.AttemptCount * 1.5) +
                    (d.TotalRatings * 2.0) +
                    d.AverageRating +
                    hotTagBonus +
                    freshnessScore;

                return new
                {
                    Deck = d,
                    Score = trendScore,
                    FreshestAt = freshestAt
                };
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.FreshestAt)
            .Take(limit)
            .ToList();

        var ownerIds = rankedDecks.Select(x => x.Deck.OwnerId).Distinct().ToList();
        var ownerLookup = await BuildOwnerLookupAsync(ownerIds);

        return rankedDecks.Select(x =>
        {
            ownerLookup.TryGetValue(x.Deck.OwnerId, out var ownerInfo);

            return new SharedDeckResponse
            {
                Id = x.Deck.Id,
                Name = x.Deck.Name,
                Description = x.Deck.Description,
                Visibility = x.Deck.Visibility,
                Status = x.Deck.Status,
                Tags = x.Deck.Tags ?? Array.Empty<string>(),
                QuestionCount = x.Deck.QuestionCount,
                ThumbnailUrl = x.Deck.ThumbnailUrl ?? string.Empty,
                CreatedAt = x.Deck.CreatedAt,
                UpdatedAt = x.Deck.UpdatedAt,
                OwnerName = ownerInfo?.FullName ?? string.Empty,
                OwnerEmail = ownerInfo?.Email ?? string.Empty,
                AverageRating = x.Deck.TotalRatings == 0 ? 0 : Math.Round(x.Deck.AverageRating, 1),
                TotalRatings = x.Deck.TotalRatings
            };
        }).ToList();
    }

    private async Task<Dictionary<string, UserInfo?>> BuildOwnerLookupAsync(IEnumerable<string> ownerIds)
    {
        var ownerLookup = new Dictionary<string, UserInfo?>();

        // Build owner lookup once to avoid repeated user-service calls per mapping branch.
        foreach (var ownerId in ownerIds)
        {
            ownerLookup[ownerId] = await _userService.GetUserInfoAsync(ownerId);
        }

        return ownerLookup;
    }
}
