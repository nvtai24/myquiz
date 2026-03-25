using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Infrastructure.Repositories;

namespace MyQuizGenerator.Infrastructure.Persistence.Repositories;

public class QuizAttemptRepository : Repository<Guid, QuizAttempt>, IQuizAttemptRepository
{
    public QuizAttemptRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<QuizAttempt>> GetAttemptsByDeckIdAsync(Guid deckId, string userId, CancellationToken cancellationToken = default)
    {
        return await _context.QuizAttempts
            .AsNoTracking()
            .Where(qa => qa.DeckId == deckId && qa.UserId == userId)
            .OrderByDescending(qa => qa.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<QuizAttempt?> GetAttemptWithAnswersAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        return await _context.QuizAttempts
            .AsNoTracking()
            .Include(qa => qa.Deck)
            .Include(qa => qa.UserAnswers)
                .ThenInclude(ua => ua.Question)
            .FirstOrDefaultAsync(qa => qa.Id == attemptId, cancellationToken);
    }
}
