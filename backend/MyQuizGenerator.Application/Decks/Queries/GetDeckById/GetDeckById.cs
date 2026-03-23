using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Decks.Queries.GetDeckById;

public record GetDeckByIdQuery(Guid Id) : IRequest<DeckDetailResponse>;

public class GetDeckByIdQueryHandler : IRequestHandler<GetDeckByIdQuery, DeckDetailResponse>
{
    private readonly IDeckRepository _deckRepository;
    private readonly IUserService _userService;

    public GetDeckByIdQueryHandler(IDeckRepository deckRepository, IUserService userService)
    {
        _deckRepository = deckRepository;
        _userService = userService;
    }

    public async Task<DeckDetailResponse> Handle(GetDeckByIdQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepository.GetDeckByIdWithQuestionsAsync(request.Id, cancellationToken);

        if (deck == null)
        {
            throw new NotFoundException(nameof(Deck), request.Id);
        }

        // Note: Access control logic (e.g. user != owner && visibility == Private) should go here if needed.
        // For now, valid deck ID returns keys.

        var ownerInfo = await _userService.GetUserInfoAsync(deck.OwnerId);

        return new DeckDetailResponse
        {
            Id = deck.Id,
            Name = deck.Name,
            Description = deck.Description,
            Visibility = deck.Visibility,
            Status = deck.Status,
            Tags = deck.Tags ?? [],
            QuestionCount = deck.Questions.Count,
            ThumbnailUrl = deck.ThumbnailUrl ?? string.Empty,
            CreatedAt = deck.CreatedAt,
            UpdatedAt = deck.UpdatedAt,
            OwnerName = ownerInfo?.FullName ?? string.Empty,
            OwnerEmail = ownerInfo?.Email ?? string.Empty,
            AverageRating = deck.DeckRatings?.Count > 0 ? Math.Round(deck.DeckRatings.Average(r => r.Rating), 1) : 0,
            TotalRatings = deck.DeckRatings?.Count ?? 0,
            Questions = deck.Questions.Select(q => new QuestionResponse
            {
                Id = q.Id,
                Content = q.Content,
                Type = q.Type,
                Hint = q.Hint,
                Explanation = q.Explanation,
                Options = q.Options,
                CorrectAnswers = q.CorrectAnswers
            }).ToList(),
            DocumentUrl = deck.DocumentUrl
        };
    }
}
