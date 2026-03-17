using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Common.Interfaces.Repositories;

public interface IDeckRepository : IRepository<Guid, Deck>
{
    Task<(List<Deck> Items, int TotalCount)> GetDecksByUserIdAsync(string userId, int page, int size, CancellationToken cancellationToken = default);
    Task<Deck?> GetDeckByIdWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasInvitationAsync(Guid deckId, string email, CancellationToken cancellationToken = default);
    Task<DeckInvitation?> GetInvitationByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task AddInvitationAsync(DeckInvitation invitation, CancellationToken cancellationToken = default);
    Task AddDeckMemberAsync(DeckMember deckMember, CancellationToken cancellationToken = default);
    Task<(List<Deck> Items, int TotalCount)> GetSharedDecksAsync(string userId, int page, int size, CancellationToken cancellationToken = default);
    Task<(List<Deck> Items, int TotalCount)> GetAttemptedDecksAsync(string userId, int page, int size, CancellationToken cancellationToken = default);
    Task<(List<Deck> Items, int TotalCount)> SearchPublicDecksAsync(string? searchTerm, int page, int size, CancellationToken cancellationToken = default);
}
