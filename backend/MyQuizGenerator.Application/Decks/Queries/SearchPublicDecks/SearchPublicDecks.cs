using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;

namespace MyQuizGenerator.Application.Decks.Queries.SearchPublicDecks;

public record SearchPublicDecksQuery(string? SearchTerm) : IRequest<List<SharedDeckResponse>>;

public class SearchPublicDecksQueryHandler : IRequestHandler<SearchPublicDecksQuery, List<SharedDeckResponse>>
{
    private readonly IDeckRepository _deckRepository;
    private readonly IUserService _userService;

    public SearchPublicDecksQueryHandler(IDeckRepository deckRepository, IUserService userService)
    {
        _deckRepository = deckRepository;
        _userService = userService;
    }

    public async Task<List<SharedDeckResponse>> Handle(SearchPublicDecksQuery request, CancellationToken cancellationToken)
    {
        var decks = await _deckRepository.SearchPublicDecksAsync(request.SearchTerm, cancellationToken);

        var result = new List<SharedDeckResponse>();

        foreach (var d in decks)
        {
            var ownerInfo = await _userService.GetUserInfoAsync(d.OwnerId);

            result.Add(new SharedDeckResponse
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

        return result;
    }
}
