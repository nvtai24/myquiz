using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.Queries.GetRecommendedDecks;

public record GetRecommendedDecksQuery(int Limit = 12) : IRequest<List<SharedDeckResponse>>;

public class GetRecommendedDecksQueryHandler : IRequestHandler<GetRecommendedDecksQuery, List<SharedDeckResponse>>
{
    private readonly IRepository<Guid, DeckView> _deckViewRepository;
    private readonly IRepository<Guid, SavedDeck> _savedDeckRepository;
    private readonly IRepository<Guid, QuizAttempt> _quizAttemptRepository;
    private readonly IRepository<Guid, Deck> _deckRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserService _userService;

    public GetRecommendedDecksQueryHandler(
        IRepository<Guid, DeckView> deckViewRepository,
        IRepository<Guid, SavedDeck> savedDeckRepository,
        IRepository<Guid, QuizAttempt> quizAttemptRepository,
        IRepository<Guid, Deck> deckRepository,
        ICurrentUserService currentUserService,
        IUserService userService)
    {
        _deckViewRepository = deckViewRepository;
        _savedDeckRepository = savedDeckRepository;
        _quizAttemptRepository = quizAttemptRepository;
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
        _userService = userService;
    }

    public async Task<List<SharedDeckResponse>> Handle(GetRecommendedDecksQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedException();
        var limit = Math.Clamp(request.Limit, 1, 30);
        var now = DateTime.UtcNow;

        // Build user-interest signals from recent behavior with different base weights.
        var viewedDecks = await _deckViewRepository.GetQueryable()
            .AsNoTracking()
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.ViewedAt)
            .Take(40)
            .Select(v => new { Tags = v.Deck.Tags, ActivityAt = v.ViewedAt, Weight = 1.0 })
            .ToListAsync(cancellationToken);

        var savedDecks = await _savedDeckRepository.GetQueryable()
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SavedAt)
            .Take(40)
            .Select(s => new { Tags = s.Deck.Tags, ActivityAt = s.SavedAt, Weight = 3.0 })
            .ToListAsync(cancellationToken);

        var studiedDecks = await _quizAttemptRepository.GetQueryable()
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.StartedAt)
            .Take(40)
            .Select(a => new { Tags = a.Deck.Tags, ActivityAt = a.StartedAt, Weight = 4.0 })
            .ToListAsync(cancellationToken);

        var tagWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var activity in viewedDecks.Concat(savedDecks).Concat(studiedDecks))
        {
            if (activity.Tags == null || activity.Tags.Length == 0)
            {
                continue;
            }

            // Recent actions should influence recommendations stronger than old actions.
            var recencyBoost = activity.ActivityAt >= now.AddDays(-7)
                ? 1.5
                : activity.ActivityAt >= now.AddDays(-30)
                    ? 1.2
                    : 1.0;

            foreach (var tag in activity.Tags.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var normalizedTag = tag.Trim();
                tagWeights[normalizedTag] = tagWeights.TryGetValue(normalizedTag, out var current)
                    ? current + (activity.Weight * recencyBoost)
                    : activity.Weight * recencyBoost;
            }
        }

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
                SaveCount = d.SavedByUsers.Count,
                ViewCount = d.DeckViews.Count,
                AttemptCount = d.QuizAttempts.Count
            })
            .ToListAsync(cancellationToken);

        // Final ranking balances tag relevance, popularity signals, and freshness.
        var rankedDecks = candidates
            .Select(d =>
            {
                var tags = d.Tags ?? Array.Empty<string>();
                var tagMatchScore = tags
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => tagWeights.TryGetValue(t.Trim(), out var weight) ? weight : 0)
                    .Sum();

                var popularityScore =
                    (d.SaveCount * 4.0) +
                    (d.ViewCount * 3.0) +
                    (d.AttemptCount * 2.0) +
                    (d.TotalRatings * 1.5) +
                    d.AverageRating;

                var freshestAt = d.UpdatedAt ?? d.CreatedAt;
                var freshnessScore = freshestAt >= now.AddDays(-7)
                    ? 8
                    : freshestAt >= now.AddDays(-30)
                        ? 4
                        : freshestAt >= now.AddDays(-90)
                            ? 2
                            : 0;

                var finalScore = (tagWeights.Count == 0 ? 0 : tagMatchScore * 3.0) + popularityScore + freshnessScore;

                return new
                {
                    Deck = d,
                    Score = finalScore,
                    TagMatchScore = tagMatchScore,
                    FreshestAt = freshestAt
                };
            })
            .OrderByDescending(x => x.TagMatchScore)
            .ThenByDescending(x => x.Score)
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

        // Batch-like lookup map so response DTOs can include owner name/email.
        foreach (var ownerId in ownerIds)
        {
            ownerLookup[ownerId] = await _userService.GetUserInfoAsync(ownerId);
        }

        return ownerLookup;
    }
}
