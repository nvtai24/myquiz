using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Decks.Queries.GetDeckMembers;

public record GetDeckMembersQuery(Guid DeckId) : IRequest<List<DeckMemberResponse>>;

public class DeckMemberResponse
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class GetDeckMembersQueryHandler : IRequestHandler<GetDeckMembersQuery, List<DeckMemberResponse>>
{
    private readonly IDeckRepository _deckRepository;
    private readonly IUserService _userService;

    public GetDeckMembersQueryHandler(IDeckRepository deckRepository, IUserService userService)
    {
        _deckRepository = deckRepository;
        _userService = userService;
    }

    public async Task<List<DeckMemberResponse>> Handle(GetDeckMembersQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepository.GetByIdAsync(request.DeckId, cancellationToken)
            ?? throw new NotFoundException(nameof(Deck), request.DeckId);

        var members = await _deckRepository.GetDeckMembersAsync(request.DeckId, cancellationToken);

        var result = new List<DeckMemberResponse>();

        foreach (var member in members)
        {
            var userInfo = await _userService.GetUserInfoAsync(member.UserId);
            result.Add(new DeckMemberResponse
            {
                Id = member.Id,
                UserId = member.UserId,
                FullName = userInfo?.FullName ?? string.Empty,
                Email = userInfo?.Email ?? string.Empty,
                JoinedAt = member.JoinedAt
            });
        }

        return result;
    }
}
