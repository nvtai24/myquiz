using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Infrastructure.Repositories;

namespace MyQuizGenerator.Infrastructure.Persistence.Repositories;

public class DeckRepository : Repository<Guid, Deck>, IDeckRepository
{
    public DeckRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Deck>> GetDecksByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Decks
            .AsNoTracking()
            .Where(d => d.OwnerId == userId)
            .Include(d => d.Questions)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Deck?> GetDeckByIdWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Decks
            .AsNoTracking()
            .Include(d => d.Questions)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }
}
