using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.User.Queries.GetUserStats;

public record GetUserStatsQuery() : IRequest<UserStatsResponse>;

public class UserStatsResponse
{
    public int TotalDecks { get; set; }
    public int TotalQuizzesAttempted { get; set; }
    public double AverageScore { get; set; }
    public int CurrentStreak { get; set; }
}

public class GetUserStatsQueryHandler : IRequestHandler<GetUserStatsQuery, UserStatsResponse>
{
    private readonly IRepository<Guid, Deck> _deckRepository;
    private readonly IRepository<Guid, DeckMember> _deckMemberRepository;
    private readonly IRepository<Guid, SavedDeck> _savedDeckRepository;
    private readonly IRepository<Guid, QuizAttempt> _quizAttemptRepository;
    private readonly ICurrentUserService _currentUser;

    public GetUserStatsQueryHandler(
        IRepository<Guid, Deck> deckRepository,
        IRepository<Guid, DeckMember> deckMemberRepository,
        IRepository<Guid, SavedDeck> savedDeckRepository,
        IRepository<Guid, QuizAttempt> quizAttemptRepository,
        ICurrentUserService currentUser)
    {
        _deckRepository = deckRepository;
        _deckMemberRepository = deckMemberRepository;
        _savedDeckRepository = savedDeckRepository;
        _quizAttemptRepository = quizAttemptRepository;
        _currentUser = currentUser;
    }

    public async Task<UserStatsResponse> Handle(GetUserStatsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!;

        // --- Total Decks ---
        // 1. Owned decks (any visibility)
        var ownedDeckIds = await _deckRepository.GetQueryable()
            .Where(d => d.OwnerId == userId)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        // 2. Member decks where deck is NOT private
        var memberDeckIds = await _deckMemberRepository.GetQueryable()
            .Where(dm => dm.UserId == userId)
            .Join(
                _deckRepository.GetQueryable().Where(d => d.Visibility != DeckVisibility.Private),
                dm => dm.DeckId,
                d => d.Id,
                (dm, d) => d.Id)
            .ToListAsync(cancellationToken);

        // 3. All deck IDs where user is a member (needed for saved filter)
        var allMemberDeckIds = await _deckMemberRepository.GetQueryable()
            .Where(dm => dm.UserId == userId)
            .Select(dm => dm.DeckId)
            .ToListAsync(cancellationToken);

        // 4. Saved decks: only count if deck is Public OR user is still a member
        var savedDeckIds = await _savedDeckRepository.GetQueryable()
            .Where(sd => sd.UserId == userId)
            .Join(
                _deckRepository.GetQueryable()
                    .Where(d => d.Visibility == DeckVisibility.Public || allMemberDeckIds.Contains(d.Id)),
                sd => sd.DeckId,
                d => d.Id,
                (sd, d) => d.Id)
            .ToListAsync(cancellationToken);

        var totalDecks = ownedDeckIds
            .Union(memberDeckIds)
            .Union(savedDeckIds)
            .Distinct()
            .Count();

        // --- Total Quizzes Attempted ---
        var attempts = await _quizAttemptRepository.GetQueryable()
            .Where(qa => qa.UserId == userId)
            .Select(qa => new { qa.CorrectAnswers, qa.TotalQuestions })
            .ToListAsync(cancellationToken);

        var totalQuizzesAttempted = attempts.Count;

        // --- Average Score ---
        double averageScore = 0;
        var validAttempts = attempts.Where(a => a.TotalQuestions > 0).ToList();
        if (validAttempts.Count > 0)
        {
            averageScore = Math.Round(
                validAttempts.Average(a => (double)a.CorrectAnswers / a.TotalQuestions * 100),
                1);
        }

        // --- Current Streak ---
        var attemptDates = await _quizAttemptRepository.GetQueryable()
            .Where(qa => qa.UserId == userId)
            .Select(qa => qa.StartedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync(cancellationToken);

        var currentStreak = CalculateStreak(attemptDates);

        return new UserStatsResponse
        {
            TotalDecks = totalDecks,
            TotalQuizzesAttempted = totalQuizzesAttempted,
            AverageScore = averageScore,
            CurrentStreak = currentStreak
        };
    }

    private static int CalculateStreak(List<DateTime> sortedDatesDesc)
    {
        if (sortedDatesDesc.Count == 0) return 0;

        var today = DateTime.UtcNow.Date;
        var streak = 0;

        // Streak starts from today or yesterday (allow today to not have activity yet)
        var checkDate = sortedDatesDesc[0] == today ? today : today.AddDays(-1);

        // If the most recent attempt is older than yesterday, streak is 0
        if (sortedDatesDesc[0] < checkDate) return 0;

        foreach (var date in sortedDatesDesc)
        {
            if (date == checkDate)
            {
                streak++;
                checkDate = checkDate.AddDays(-1);
            }
            else if (date < checkDate)
            {
                break;
            }
        }

        return streak;
    }
}
