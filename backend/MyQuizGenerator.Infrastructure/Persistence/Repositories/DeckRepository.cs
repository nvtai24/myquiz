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

    public async Task<bool> HasInvitationAsync(Guid deckId, string email, CancellationToken cancellationToken = default)
    {
        return await _context.DeckInvitations
            .AnyAsync(i => i.DeckId == deckId && i.Email == email && i.Status == Domain.Enums.DeckInvitationStatus.Pending, cancellationToken);
    }

    public async Task<DeckInvitation?> GetInvitationByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.DeckInvitations
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
    }

    public async Task AddInvitationAsync(DeckInvitation invitation, CancellationToken cancellationToken = default)
    {
        await _context.DeckInvitations.AddAsync(invitation, cancellationToken);
    }

    public async Task AddDeckMemberAsync(DeckMember deckMember, CancellationToken cancellationToken = default)
    {
        await _context.DeckMembers.AddAsync(deckMember, cancellationToken);
    }

    public async Task<List<Deck>> GetSharedDecksAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Decks
            .AsNoTracking()
            .Where(d => d.DeckMembers.Any(dm => dm.UserId == userId))
            .Include(d => d.Questions)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Deck>> GetAttemptedDecksAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.QuizAttempts
            .AsNoTracking()
            .Where(qa => qa.UserId == userId)
            .GroupBy(qa => qa.DeckId)
            .Select(g => new { DeckId = g.Key, LatestAttempt = g.Max(qa => qa.StartedAt) })
            .OrderByDescending(x => x.LatestAttempt)
            .Join(
                _context.Decks.AsNoTracking().Include(d => d.Questions),
                attempt => attempt.DeckId,
                deck => deck.Id,
                (attempt, deck) => deck)
            .ToListAsync(cancellationToken);
    }
}
