using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.DeckRatings.DTOs;

namespace MyQuizGenerator.Application.DeckRatings.Queries.GetDeckRatings;

public record GetDeckRatingsQuery(Guid DeckId) : IRequest<DeckRatingSummaryResponse>;

public class GetDeckRatingsQueryHandler : IRequestHandler<GetDeckRatingsQuery, DeckRatingSummaryResponse>
{
    private readonly IDeckRatingRepository _deckRatingRepository;
    private readonly IUserService _userService;

    public GetDeckRatingsQueryHandler(IDeckRatingRepository deckRatingRepository, IUserService userService)
    {
        _deckRatingRepository = deckRatingRepository;
        _userService = userService;
    }

    public async Task<DeckRatingSummaryResponse> Handle(GetDeckRatingsQuery request, CancellationToken cancellationToken)
    {
        var ratings = await _deckRatingRepository.GetRatingsByDeckIdAsync(request.DeckId, cancellationToken);
        var averageRating = await _deckRatingRepository.GetAverageRatingAsync(request.DeckId, cancellationToken);
        var totalRatings = await _deckRatingRepository.GetRatingCountAsync(request.DeckId, cancellationToken);

        var ratingResponses = new List<DeckRatingResponse>();

        foreach (var r in ratings)
        {
            var userInfo = await _userService.GetUserInfoAsync(r.UserId);

            ratingResponses.Add(new DeckRatingResponse
            {
                Id = r.Id,
                DeckId = r.DeckId,
                UserId = r.UserId,
                UserName = userInfo?.FullName ?? string.Empty,
                UserEmail = userInfo?.Email ?? string.Empty,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            });
        }

        return new DeckRatingSummaryResponse
        {
            AverageRating = Math.Round(averageRating, 1),
            TotalRatings = totalRatings,
            Ratings = ratingResponses
        };
    }
}
