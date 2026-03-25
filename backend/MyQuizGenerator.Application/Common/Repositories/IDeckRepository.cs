using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Common.Interfaces.Repositories;

public interface IDeckRepository : IRepository<Guid, Deck>
{
    Task<(List<Deck> Items, int TotalCount)> GetDecksByUserIdAsync(string userId, int page, int size, CancellationToken cancellationToken = default);
    Task<(List<Deck> Items, int TotalCount)> GetMyPublishedDecksAsync(string userId, int page, int size, CancellationToken cancellationToken = default);
    Task<(List<Deck> Items, int TotalCount)> GetMyDraftsAsync(string userId, int page, int size, CancellationToken cancellationToken = default);
    Task<Deck?> GetDeckByIdWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasInvitationAsync(Guid deckId, string email, CancellationToken cancellationToken = default);
    Task<DeckInvitation?> GetInvitationByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task AddInvitationAsync(DeckInvitation invitation, CancellationToken cancellationToken = default);
    Task AddDeckMemberAsync(DeckMember deckMember, CancellationToken cancellationToken = default);
    Task<bool> IsMemberAsync(Guid deckId, string userId, CancellationToken cancellationToken = default);
    Task<List<DeckMember>> GetDeckMembersAsync(Guid deckId, CancellationToken cancellationToken = default);
    Task<(List<Deck> Items, int TotalCount)> GetSharedDecksAsync(string userId, int page, int size, CancellationToken cancellationToken = default);
    Task<(List<Deck> Items, int TotalCount)> GetAttemptedDecksAsync(string userId, int page, int size, CancellationToken cancellationToken = default);
    Task<(List<Deck> Items, int TotalCount)> SearchPublicDecksAsync(string? searchTerm, int page, int size, CancellationToken cancellationToken = default);

    // Question management
    Task<List<Question>> GetQuestionsByIdsAsync(IEnumerable<int> ids, Guid deckId, CancellationToken cancellationToken = default);
    Task AddQuestionsAsync(IEnumerable<Question> questions, CancellationToken cancellationToken = default);

    // Saved decks
    Task<bool> IsSavedAsync(Guid deckId, string userId, CancellationToken cancellationToken = default);
    Task<SavedDeck?> GetSavedDeckAsync(Guid deckId, string userId, CancellationToken cancellationToken = default);
    Task AddSavedDeckAsync(SavedDeck savedDeck, CancellationToken cancellationToken = default);
    void RemoveSavedDeck(SavedDeck savedDeck);
    Task<(List<Deck> Items, int TotalCount)> GetSavedDecksAsync(string userId, int page, int size, CancellationToken cancellationToken = default);
}
