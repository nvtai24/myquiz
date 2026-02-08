using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.Commands.CreateDeck;

public record CreateDeckCommand(
    CreateDeckRequest Request
) : IRequest<Guid>;



public class CreateDeckCommandHandler : IRequestHandler<CreateDeckCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Guid, Deck> _deckRepository;
    private readonly IRepository<int, Question> _questionRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateDeckCommandHandler(
        IUnitOfWork unitOfWork,
        IRepository<Guid, Deck> deckRepository,
        IRepository<int, Question> questionRepository,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _deckRepository = deckRepository;
        _questionRepository = questionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateDeckCommand request, CancellationToken cancellationToken)
    {
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
            Source = DeckSource.Manual,
            Tags = request.Request.Tags,
            OwnerId = _currentUserService.UserId ?? string.Empty,
            Questions = listQuestions
        };

        await _deckRepository.AddAsync(deck, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return deck.Id;
    }
}
