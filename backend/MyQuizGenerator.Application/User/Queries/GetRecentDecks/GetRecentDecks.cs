using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.User.Queries.GetRecentDecks;

public record GetRecentDecksQuery(int Limit = 4) : IRequest<List<RecentDeckResponse>>;

public class RecentDeckResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public int QuestionCount { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty; // "Viewed" | "Studied" | "Saved"
    public DateTime LastActivityAt { get; set; }
}

public class GetRecentDecksQueryHandler : IRequestHandler<GetRecentDecksQuery, List<RecentDeckResponse>>
{
    private readonly IRepository<Guid, DeckView> _deckViewRepository;
    private readonly IRepository<Guid, QuizAttempt> _quizAttemptRepository;
    private readonly IRepository<Guid, SavedDeck> _savedDeckRepository;
    private readonly IRepository<Guid, DeckMember> _deckMemberRepository;
    private readonly IRepository<Guid, Deck> _deckRepository;
    private readonly ICurrentUserService _currentUser;

    public GetRecentDecksQueryHandler(
        IRepository<Guid, DeckView> deckViewRepository,
        IRepository<Guid, QuizAttempt> quizAttemptRepository,
        IRepository<Guid, SavedDeck> savedDeckRepository,
        IRepository<Guid, DeckMember> deckMemberRepository,
        IRepository<Guid, Deck> deckRepository,
        ICurrentUserService currentUser)
    {
        _deckViewRepository = deckViewRepository;
        _quizAttemptRepository = quizAttemptRepository;
        _savedDeckRepository = savedDeckRepository;
        _deckMemberRepository = deckMemberRepository;
        _deckRepository = deckRepository;
        _currentUser = currentUser;
    }

    public async Task<List<RecentDeckResponse>> Handle(GetRecentDecksQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!;

        // Get deck IDs user is a member of (for access checks)
        var memberDeckIds = await _deckMemberRepository.GetQueryable()
            .Where(dm => dm.UserId == userId)
            .Select(dm => dm.DeckId)
            .ToListAsync(cancellationToken);

        // Recent views
        var recentViews = await _deckViewRepository.GetQueryable()
            .Where(v => v.UserId == userId)
            .Select(v => new { v.DeckId, ActivityAt = v.ViewedAt, ActivityType = "Viewed" })
            .ToListAsync(cancellationToken);

        // Recent quiz attempts (take most recent per deck)
        var recentStudied = await _quizAttemptRepository.GetQueryable()
            .Where(qa => qa.UserId == userId)
            .GroupBy(qa => qa.DeckId)
            .Select(g => new { DeckId = g.Key, ActivityAt = g.Max(qa => qa.StartedAt), ActivityType = "Studied" })
            .ToListAsync(cancellationToken);

        // Recent saves
        var recentSaved = await _savedDeckRepository.GetQueryable()
            .Where(sd => sd.UserId == userId)
            .Select(sd => new { sd.DeckId, ActivityAt = sd.SavedAt, ActivityType = "Saved" })
            .ToListAsync(cancellationToken);

        // Union all activities, pick most recent activity per deck
        var allActivities = recentViews
            .Select(v => new { v.DeckId, v.ActivityAt, v.ActivityType })
            .Concat(recentStudied.Select(s => new { s.DeckId, s.ActivityAt, s.ActivityType }))
            .Concat(recentSaved.Select(s => new { s.DeckId, s.ActivityAt, s.ActivityType }))
            .GroupBy(a => a.DeckId)
            .Select(g => g.OrderByDescending(a => a.ActivityAt).First())
            .OrderByDescending(a => a.ActivityAt)
            .Take(request.Limit * 3) // fetch more to account for filtering
            .ToList();

        var candidateDeckIds = allActivities.Select(a => a.DeckId).ToList();

        // Load deck details with access filter: owned OR public OR (shared and is member)
        var decks = await _deckRepository.GetQueryable()
            .Where(d => candidateDeckIds.Contains(d.Id)
                && (d.OwnerId == userId
                    || d.Visibility == DeckVisibility.Public
                    || (d.Visibility == DeckVisibility.Shared && memberDeckIds.Contains(d.Id))))
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Description,
                d.ThumbnailUrl,
                d.Tags,
                d.OwnerId,
                QuestionCount = d.Questions.Count
            })
            .ToListAsync(cancellationToken);

        var deckLookup = decks.ToDictionary(d => d.Id);

        return allActivities
            .Where(a => deckLookup.ContainsKey(a.DeckId))
            .Take(request.Limit)
            .Select(a =>
            {
                var d = deckLookup[a.DeckId];
                return new RecentDeckResponse
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    ThumbnailUrl = d.ThumbnailUrl,
                    Tags = d.Tags ?? Array.Empty<string>(),
                    QuestionCount = d.QuestionCount,
                    OwnerId = d.OwnerId,
                    ActivityType = a.ActivityType,
                    LastActivityAt = a.ActivityAt
                };
            })
            .ToList();
    }
}
