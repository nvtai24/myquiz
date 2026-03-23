using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Application.Files.DTOs;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.Commands.CreateDeck;

public record CreateDeckCommand(CreateDeckRequest Request) : IRequest<Guid>;

public class CreateDeckCommandHandler : IRequestHandler<CreateDeckCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileService _fileService;
    private readonly IRepository<Guid, UserSubscriptionPlan> _userSubscriptionRepository;

    public CreateDeckCommandHandler(
        IUnitOfWork unitOfWork,
        IDeckRepository deckRepository,
        ICurrentUserService currentUserService,
        IFileService fileService,
        IRepository<Guid, UserSubscriptionPlan> userSubscriptionRepository)
    {
        _unitOfWork = unitOfWork;
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
        _fileService = fileService;
        _userSubscriptionRepository = userSubscriptionRepository;
    }

    public async Task<Guid> Handle(CreateDeckCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? string.Empty;

        // Get user's active subscription plan with highest priority (Order)
        var now = DateTime.UtcNow;
        var activePlan = await _userSubscriptionRepository.GetQueryable()
            .Include(usp => usp.SubscriptionPlan)
            .Where(usp => usp.UserId == userId && usp.StartDate <= now && usp.EndDate > now)
            .OrderByDescending(usp => usp.SubscriptionPlan.Order)
            .Select(usp => usp.SubscriptionPlan)
            .FirstOrDefaultAsync(cancellationToken);

        if (activePlan != null && activePlan.NumDeckLimit > 0)
        {
            var currentDeckCount = await _deckRepository.GetQueryable()
                .CountAsync(d => d.OwnerId == userId, cancellationToken);

            if (currentDeckCount >= activePlan.NumDeckLimit)
            {
                throw new BadRequestException(
                    $"You have reached the maximum number of decks ({activePlan.NumDeckLimit}) allowed by your current plan ({activePlan.Name}). Please upgrade your plan to create more decks.");
            }
        }

        var listQuestions = new List<Question>();
        var deckId = Guid.NewGuid();

        foreach (var item in request.Request.Questions)
        {
            listQuestions.Add(new Question
            {
                Content = item.Content,
                Type = item.Type,
                Hint = item.Hint,
                Explanation = item.Explanation,
                Options = item.Options,
                CorrectAnswers = item.CorrectAnswers,
                DeckId = deckId
            });
        }

        var deck = new Deck
        {
            Id = deckId,
            Name = request.Request.Name,
            Description = request.Request.Description,
            Visibility = request.Request.Visibility,
            Status = request.Request.Status,
            Source = request.Request.Source,
            Tags = request.Request.Tags,
            OwnerId = userId,
            Questions = listQuestions,
            ThumbnailUrl = request.Request.ThumbnailUrl,
            DocumentUrl = request.Request.DocumentUrl
        };

        await _deckRepository.AddAsync(deck, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return deck.Id;
    }
}
