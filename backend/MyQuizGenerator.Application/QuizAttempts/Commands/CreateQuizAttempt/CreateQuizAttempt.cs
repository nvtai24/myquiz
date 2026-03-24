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
    private readonly ICurrentUserService _currentUserService;

    public CreateQuizAttemptCommandHandler(
        IUnitOfWork unitOfWork,
        IQuizAttemptRepository quizAttemptRepository,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _quizAttemptRepository = quizAttemptRepository;
        _currentUserService = currentUserService;
    }

    public async Task<QuizAttemptResponse> Handle(CreateQuizAttemptCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var attemptId = Guid.NewGuid();

        var userAnswers = command.Request.UserAnswers.Select(ua =>
        {
            return new UserAnswer
            {
                Id = Guid.NewGuid(),
                QuizAttemptId = attemptId,
                QuestionId = ua.QuestionId,
                QuestionSnapshot = ua.QuestionSnapshot,
                OptionsSnapshot = ua.OptionsSnapshot,
                CorrectAnswersSnapshot = ua.CorrectAnswersSnapshot,
                Answer = ua.Answer,
                IsCorrect = ua.IsCorrect
            };
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
                Question = ua.QuestionSnapshot,
                Options = ua.OptionsSnapshot,
                CorrectAnswers = ua.CorrectAnswersSnapshot,
                Answer = ua.Answer,
                IsCorrect = ua.IsCorrect
            }).ToList()
        };
    }
}
