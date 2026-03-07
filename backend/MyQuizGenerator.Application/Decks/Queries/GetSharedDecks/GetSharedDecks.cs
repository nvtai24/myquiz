using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;

namespace MyQuizGenerator.Application.Decks.Queries.GetSharedDecks;

public record GetSharedDecksQuery(string UserId) : IRequest<List<SharedDeckResponse>>;

public class GetSharedDecksQueryHandler : IRequestHandler<GetSharedDecksQuery, List<SharedDeckResponse>>
{
    private readonly IDeckRepository _deckRepository;
    private readonly IUserService _userService;

    public GetSharedDecksQueryHandler(IDeckRepository deckRepository, IUserService userService)
    {
        _deckRepository = deckRepository;
        _userService = userService;
    }

    public async Task<List<SharedDeckResponse>> Handle(GetSharedDecksQuery request, CancellationToken cancellationToken)
    {
        var decks = await _deckRepository.GetSharedDecksAsync(request.UserId, cancellationToken);

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
                OwnerEmail = ownerInfo?.Email ?? string.Empty
            });
        }

        return result;
    }
}
