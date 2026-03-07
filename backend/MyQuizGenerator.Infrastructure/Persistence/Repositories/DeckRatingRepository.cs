using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Infrastructure.Repositories;

namespace MyQuizGenerator.Infrastructure.Persistence.Repositories;

public class DeckRatingRepository : Repository<Guid, DeckRating>, IDeckRatingRepository
{
    public DeckRatingRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<DeckRating>> GetRatingsByDeckIdAsync(Guid deckId, CancellationToken cancellationToken = default)
    {
        return await _context.DeckRatings
            .AsNoTracking()
            .Where(r => r.DeckId == deckId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeckRating?> GetUserRatingForDeckAsync(Guid deckId, string userId, CancellationToken cancellationToken = default)
    {
        return await _context.DeckRatings
            .FirstOrDefaultAsync(r => r.DeckId == deckId && r.UserId == userId, cancellationToken);
    }

    public async Task<double> GetAverageRatingAsync(Guid deckId, CancellationToken cancellationToken = default)
    {
        var ratings = await _context.DeckRatings
            .Where(r => r.DeckId == deckId)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        return ratings.Count == 0 ? 0 : ratings.Average();
    }

    public async Task<int> GetRatingCountAsync(Guid deckId, CancellationToken cancellationToken = default)
    {
        return await _context.DeckRatings
            .CountAsync(r => r.DeckId == deckId, cancellationToken);
    }
}
