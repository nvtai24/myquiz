using MediatR;
using MyQuizGenerator.Application.Common.DTOs;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;

namespace MyQuizGenerator.Application.Decks.Queries.GetSharedDecks;

public record GetSharedDecksQuery(string UserId, int Page = 1, int Size = 10) : IRequest<PagedResult<SharedDeckResponse>>;

public class GetSharedDecksQueryHandler : IRequestHandler<GetSharedDecksQuery, PagedResult<SharedDeckResponse>>
{
    private readonly IDeckRepository _deckRepository;
    private readonly IUserService _userService;

    public GetSharedDecksQueryHandler(IDeckRepository deckRepository, IUserService userService)
    {
        _deckRepository = deckRepository;
        _userService = userService;
    }

    public async Task<PagedResult<SharedDeckResponse>> Handle(GetSharedDecksQuery request, CancellationToken cancellationToken)
    {
        var (decks, totalCount) = await _deckRepository.GetSharedDecksAsync(request.UserId, request.Page, request.Size, cancellationToken);

        var items = new List<SharedDeckResponse>();

        foreach (var d in decks)
        {
            var ownerInfo = await _userService.GetUserInfoAsync(d.OwnerId);

            items.Add(new SharedDeckResponse
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                Visibility = d.Visibility,
                Tags = d.Tags,
                QuestionCount = d.Questions.Count,
                ThumbnailUrl = d.ThumbnailUrl,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                OwnerName = ownerInfo?.FullName ?? string.Empty,
                OwnerEmail = ownerInfo?.Email ?? string.Empty,
                AverageRating = d.DeckRatings.Count != 0 ? Math.Round(d.DeckRatings.Average(r => r.Rating), 1) : 0,
                TotalRatings = d.DeckRatings.Count
            });
        }

        return new PagedResult<SharedDeckResponse>(items, request.Page, request.Size, totalCount);
    }
}
