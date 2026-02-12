using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.QuizAttempts.DTOs;

namespace MyQuizGenerator.Application.QuizAttempts.Queries.GetQuizAttemptById;

public record GetQuizAttemptByIdQuery(Guid AttemptId) : IRequest<QuizAttemptResponse>;

public class GetQuizAttemptByIdQueryHandler : IRequestHandler<GetQuizAttemptByIdQuery, QuizAttemptResponse>
{
    private readonly IQuizAttemptRepository _quizAttemptRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetQuizAttemptByIdQueryHandler(
        IQuizAttemptRepository quizAttemptRepository,
        ICurrentUserService currentUserService)
    {
        _quizAttemptRepository = quizAttemptRepository;
        _currentUserService = currentUserService;
    }

    public async Task<QuizAttemptResponse> Handle(GetQuizAttemptByIdQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var attempt = await _quizAttemptRepository.GetAttemptWithAnswersAsync(query.AttemptId, cancellationToken)
            ?? throw new NotFoundException("QuizAttempt", query.AttemptId);

        if (attempt.UserId != userId)
            throw new ForbiddenException("You do not have access to this quiz attempt.");

        return new QuizAttemptResponse
        {
            Id = attempt.Id,
            DeckId = attempt.DeckId,
            UserId = attempt.UserId,
            StartedAt = attempt.StartedAt,
            EndedAt = attempt.EndedAt,
            TotalTime = attempt.TotalTime,
            TotalQuestions = attempt.TotalQuestions,
            CorrectAnswers = attempt.CorrectAnswers,
            Score = attempt.TotalQuestions > 0 ? Math.Round((double)attempt.CorrectAnswers / attempt.TotalQuestions * 100, 2) : 0,
            UserAnswers = attempt.UserAnswers.Select(ua => new UserAnswerResponse
            {
                Id = ua.Id,
                QuestionId = ua.QuestionId,
                Question = ua.Question.Content,
                Options = ua.Question.Options,
                CorrectAnswers = ua.Question.CorrectAnswers,
                Answer = ua.Answer,
                IsCorrect = ua.IsCorrect
            }).ToList()
        };
    }
}
