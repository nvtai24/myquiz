using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Decks.Commands.CreateDeck;

public record CreateDeckCommand(CreateDeckRequest Request) : IRequest<Guid>;

public class CreateDeckCommandHandler : IRequestHandler<CreateDeckCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateDeckCommandHandler(
        IUnitOfWork unitOfWork,
        IDeckRepository deckRepository,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateDeckCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? string.Empty;

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
