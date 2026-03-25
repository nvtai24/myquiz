using MediatR;
using MyQuizGenerator.Application.Common.DTOs;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;

namespace MyQuizGenerator.Application.Decks.Queries.GetAttemptedDecks;

public record GetAttemptedDecksQuery(string UserId, int Page = 1, int Size = 10) : IRequest<PagedResult<QuizAttemptHistoryResponse>>;

public class GetAttemptedDecksQueryHandler : IRequestHandler<GetAttemptedDecksQuery, PagedResult<QuizAttemptHistoryResponse>>
{
    private readonly IDeckRepository _deckRepository;

    public GetAttemptedDecksQueryHandler(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task<PagedResult<QuizAttemptHistoryResponse>> Handle(GetAttemptedDecksQuery request, CancellationToken cancellationToken)
    {
        var (attempts, totalCount) = await _deckRepository.GetAttemptedDecksAsync(request.UserId, request.Page, request.Size, cancellationToken);

        var items = attempts.Select(qa => new QuizAttemptHistoryResponse
        {
            AttemptId = qa.Id,
            DeckId = qa.DeckId,
            DeckName = qa.Deck.Name,
            DeckThumbnailUrl = qa.Deck.ThumbnailUrl ?? string.Empty,
            TotalQuestions = qa.TotalQuestions,
            CorrectAnswers = qa.CorrectAnswers,
            ScorePercent = qa.TotalQuestions > 0
                ? Math.Round((double)qa.CorrectAnswers / qa.TotalQuestions * 100, 1)
                : 0,
            TotalTime = qa.TotalTime,
            StartedAt = qa.StartedAt,
            EndedAt = qa.EndedAt,
        }).ToList();

        return new PagedResult<QuizAttemptHistoryResponse>(items, request.Page, request.Size, totalCount);
    }
}
