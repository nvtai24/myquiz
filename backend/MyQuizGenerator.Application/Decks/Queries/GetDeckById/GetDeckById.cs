using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.Queries.GetDeckById;

public record GetDeckByIdQuery(Guid Id) : IRequest<DeckDetailResponse>;

public class GetDeckByIdQueryHandler : IRequestHandler<GetDeckByIdQuery, DeckDetailResponse>
{
    private readonly IDeckRepository _deckRepository;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public GetDeckByIdQueryHandler(
        IDeckRepository deckRepository,
        IUserService userService,
        ICurrentUserService currentUserService)
    {
        _deckRepository = deckRepository;
        _userService = userService;
        _currentUserService = currentUserService;
    }

    public async Task<DeckDetailResponse> Handle(GetDeckByIdQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepository.GetDeckByIdWithQuestionsAsync(request.Id, cancellationToken);

        if (deck == null)
        {
            throw new NotFoundException(nameof(Deck), request.Id);
        }

        // Draft decks: only owner can view
        if (deck.Status == DeckStatus.Draft)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId) || deck.OwnerId != userId)
            {
                throw new ForbiddenException("This deck is not published yet.");
            }
        }

        // Access control based on visibility
        if (deck.Visibility != DeckVisibility.Public)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrEmpty(userId))
            {
                throw new ForbiddenException("You do not have access to this deck.");
            }

            if (deck.Visibility == DeckVisibility.Private)
            {
                // Private: only owner can view
                if (deck.OwnerId != userId)
                {
                    throw new ForbiddenException("You do not have access to this deck.");
                }
            }
            else if (deck.Visibility == DeckVisibility.Shared)
            {
                // Shared: owner or member
                if (deck.OwnerId != userId)
                {
                    var isMember = await _deckRepository.IsMemberAsync(deck.Id, userId, cancellationToken);
                    if (!isMember)
                    {
                        throw new ForbiddenException("You do not have access to this deck.");
                    }
                }
            }
        }

        var ownerInfo = await _userService.GetUserInfoAsync(deck.OwnerId);

        var currentUserId = _currentUserService.UserId;
        var isSaved = !string.IsNullOrEmpty(currentUserId)
            && await _deckRepository.IsSavedAsync(deck.Id, currentUserId, cancellationToken);

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
            DocumentUrl = deck.DocumentUrl,
            IsSaved = isSaved
        };
    }
}

