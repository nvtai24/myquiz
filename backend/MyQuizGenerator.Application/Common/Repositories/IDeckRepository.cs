using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Common.Interfaces.Repositories;

public interface IDeckRepository : IRepository<Guid, Deck>
{
    Task<List<Deck>> GetDecksByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Deck?> GetDeckByIdWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasInvitationAsync(Guid deckId, string email, CancellationToken cancellationToken = default);
    Task AddInvitationAsync(DeckInvitation invitation, CancellationToken cancellationToken = default);
}
