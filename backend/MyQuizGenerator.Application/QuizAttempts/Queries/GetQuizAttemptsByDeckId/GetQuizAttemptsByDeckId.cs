using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.QuizAttempts.DTOs;

namespace MyQuizGenerator.Application.QuizAttempts.Queries.GetQuizAttemptsByDeckId;

public record GetQuizAttemptsByDeckIdQuery(Guid DeckId) : IRequest<List<QuizAttemptResponse>>;

public class GetQuizAttemptsByDeckIdQueryHandler : IRequestHandler<GetQuizAttemptsByDeckIdQuery, List<QuizAttemptResponse>>
{
    private readonly IQuizAttemptRepository _quizAttemptRepository;
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetQuizAttemptsByDeckIdQueryHandler(
        IQuizAttemptRepository quizAttemptRepository,
        IDeckRepository deckRepository,
        ICurrentUserService currentUserService)
    {
        _quizAttemptRepository = quizAttemptRepository;
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<QuizAttemptResponse>> Handle(GetQuizAttemptsByDeckIdQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var deck = await _deckRepository.GetByIdAsync(query.DeckId, cancellationToken)
            ?? throw new NotFoundException("Deck", query.DeckId);

        var attempts = await _quizAttemptRepository.GetAttemptsByDeckIdAsync(query.DeckId, userId, cancellationToken);

        return attempts.Select(a => new QuizAttemptResponse
        {
            Id = a.Id,
            DeckId = a.DeckId,
            UserId = a.UserId,
            StartedAt = a.StartedAt,
            EndedAt = a.EndedAt,
            TotalTime = a.TotalTime,
            TotalQuestions = a.TotalQuestions,
            CorrectAnswers = a.CorrectAnswers,
            Score = a.TotalQuestions > 0 ? Math.Round((double)a.CorrectAnswers / a.TotalQuestions * 100, 2) : 0
        }).ToList();
    }
}
