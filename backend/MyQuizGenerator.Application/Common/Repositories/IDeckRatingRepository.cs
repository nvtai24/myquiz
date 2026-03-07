using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Common.Interfaces.Repositories;

public interface IDeckRatingRepository : IRepository<Guid, DeckRating>
{
    Task<List<DeckRating>> GetRatingsByDeckIdAsync(Guid deckId, CancellationToken cancellationToken = default);
    Task<DeckRating?> GetUserRatingForDeckAsync(Guid deckId, string userId, CancellationToken cancellationToken = default);
    Task<double> GetAverageRatingAsync(Guid deckId, CancellationToken cancellationToken = default);
    Task<int> GetRatingCountAsync(Guid deckId, CancellationToken cancellationToken = default);
}
