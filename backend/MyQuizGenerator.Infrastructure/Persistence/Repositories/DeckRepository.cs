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

    public async Task<(List<Deck> Items, int TotalCount)> GetDecksByUserIdAsync(string userId, int page, int size, CancellationToken cancellationToken = default)
    {
        var query = _context.Decks
            .AsNoTracking()
            .Where(d => d.OwnerId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(d => d.Questions)
            .Include(d => d.DeckRatings)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Deck?> GetDeckByIdWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Decks
            .AsNoTracking()
            .Include(d => d.Questions)
            .Include(d => d.DeckRatings)
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

    public async Task<(List<Deck> Items, int TotalCount)> GetSharedDecksAsync(string userId, int page, int size, CancellationToken cancellationToken = default)
    {
        var query = _context.Decks
            .AsNoTracking()
            .Where(d => d.DeckMembers.Any(dm => dm.UserId == userId));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(d => d.Questions)
            .Include(d => d.DeckRatings)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<Deck> Items, int TotalCount)> GetAttemptedDecksAsync(string userId, int page, int size, CancellationToken cancellationToken = default)
    {
        var baseQuery = _context.QuizAttempts
            .AsNoTracking()
            .Where(qa => qa.UserId == userId)
            .GroupBy(qa => qa.DeckId)
            .Select(g => new { DeckId = g.Key, LatestAttempt = g.Max(qa => qa.StartedAt) });

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var deckIds = await baseQuery
            .OrderByDescending(x => x.LatestAttempt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => x.DeckId)
            .ToListAsync(cancellationToken);

        var items = await _context.Decks
            .AsNoTracking()
            .Where(d => deckIds.Contains(d.Id))
            .Include(d => d.Questions)
            .Include(d => d.DeckRatings)
            .ToListAsync(cancellationToken);

        // Preserve the order from deckIds
        var orderedItems = deckIds
            .Select(id => items.First(d => d.Id == id))
            .ToList();

        return (orderedItems, totalCount);
    }

    public async Task<(List<Deck> Items, int TotalCount)> SearchPublicDecksAsync(string? searchTerm, int page, int size, CancellationToken cancellationToken = default)
    {
        var query = _context.Decks
            .AsNoTracking()
            .Where(d => d.Visibility == Domain.Enums.DeckVisibility.Public);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(term) ||
                d.Description.ToLower().Contains(term) ||
                d.Tags.Any(t => t.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(d => d.Questions)
            .Include(d => d.DeckRatings)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
