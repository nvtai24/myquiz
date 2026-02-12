using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Common.Interfaces.Repositories;

public interface IQuizAttemptRepository : IRepository<Guid, QuizAttempt>
{
    Task<List<QuizAttempt>> GetAttemptsByDeckIdAsync(Guid deckId, string userId, CancellationToken cancellationToken = default);
    Task<QuizAttempt?> GetAttemptWithAnswersAsync(Guid attemptId, CancellationToken cancellationToken = default);
}
