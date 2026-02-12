using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.QuizAttempts.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.QuizAttempts.Commands.CreateQuizAttempt;

public record CreateQuizAttemptCommand(CreateQuizAttemptRequest Request) : IRequest<QuizAttemptResponse>;

public class CreateQuizAttemptCommandHandler : IRequestHandler<CreateQuizAttemptCommand, QuizAttemptResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQuizAttemptRepository _quizAttemptRepository;
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateQuizAttemptCommandHandler(
        IUnitOfWork unitOfWork,
        IQuizAttemptRepository quizAttemptRepository,
        IDeckRepository deckRepository,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _quizAttemptRepository = quizAttemptRepository;
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
    }

    public async Task<QuizAttemptResponse> Handle(CreateQuizAttemptCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        // Verify deck exists
        var deck = await _deckRepository.GetByIdAsync(command.Request.DeckId, cancellationToken)
            ?? throw new NotFoundException("Deck", command.Request.DeckId);

        var attemptId = Guid.NewGuid();

        var userAnswers = command.Request.UserAnswers.Select(ua => new UserAnswer
        {
            Id = Guid.NewGuid(),
            QuizAttemptId = attemptId,
            QuestionId = ua.QuestionId,
            Answer = ua.Answer,
            IsCorrect = ua.IsCorrect
        }).ToList();

        var correctCount = userAnswers.Count(ua => ua.IsCorrect);
        var totalQuestions = userAnswers.Count;
        var score = totalQuestions > 0 ? Math.Round((double)correctCount / totalQuestions * 100, 2) : 0;

        var quizAttempt = new QuizAttempt
        {
            Id = attemptId,
            DeckId = command.Request.DeckId,
            UserId = userId,
            StartedAt = command.Request.StartedAt,
            EndedAt = command.Request.EndedAt,
            TotalTime = command.Request.TotalTime,
            TotalQuestions = totalQuestions,
            CorrectAnswers = correctCount,
            UserAnswers = userAnswers
        };

        await _quizAttemptRepository.AddAsync(quizAttempt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new QuizAttemptResponse
        {
            Id = quizAttempt.Id,
            DeckId = quizAttempt.DeckId,
            UserId = quizAttempt.UserId,
            StartedAt = quizAttempt.StartedAt,
            EndedAt = quizAttempt.EndedAt,
            TotalTime = quizAttempt.TotalTime,
            TotalQuestions = quizAttempt.TotalQuestions,
            CorrectAnswers = quizAttempt.CorrectAnswers,
            Score = score,
            UserAnswers = quizAttempt.UserAnswers.Select(ua => new UserAnswerResponse
            {
                Id = ua.Id,
                QuestionId = ua.QuestionId,
                Answer = ua.Answer,
                IsCorrect = ua.IsCorrect
            }).ToList()
        };
    }
}
