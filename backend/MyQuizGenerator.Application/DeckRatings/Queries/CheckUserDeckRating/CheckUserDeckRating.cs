using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;

namespace MyQuizGenerator.Application.DeckRatings.Queries.CheckUserDeckRating;

public record CheckUserDeckRatingQuery(Guid DeckId) : IRequest<CheckUserDeckRatingResponse>;

public class CheckUserDeckRatingResponse
{
    public bool HasRated { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
}

public class CheckUserDeckRatingQueryHandler : IRequestHandler<CheckUserDeckRatingQuery, CheckUserDeckRatingResponse>
{
    private readonly IDeckRatingRepository _deckRatingRepository;
    private readonly ICurrentUserService _currentUserService;

    public CheckUserDeckRatingQueryHandler(
        IDeckRatingRepository deckRatingRepository,
        ICurrentUserService currentUserService)
    {
        _deckRatingRepository = deckRatingRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CheckUserDeckRatingResponse> Handle(CheckUserDeckRatingQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var existing = await _deckRatingRepository.GetUserRatingForDeckAsync(request.DeckId, userId, cancellationToken);

        return new CheckUserDeckRatingResponse
        {
            HasRated = existing != null,
            Rating = existing?.Rating,
            Comment = existing?.Comment
        };
    }
}
