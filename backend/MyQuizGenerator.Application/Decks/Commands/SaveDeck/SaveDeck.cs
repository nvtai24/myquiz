using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Common.Services;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Decks.Commands.SaveDeck;

public record SaveDeckCommand(Guid DeckId) : IRequest;

public class SaveDeckCommandHandler : IRequestHandler<SaveDeckCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;

    public SaveDeckCommandHandler(
        IUnitOfWork unitOfWork,
        IDeckRepository deckRepository,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(SaveDeckCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException();

        var deck = await _deckRepository.GetByIdAsync(request.DeckId, cancellationToken)
            ?? throw new NotFoundException(nameof(Deck), request.DeckId);

        var isSaved = await _deckRepository.IsSavedAsync(request.DeckId, userId, cancellationToken);
        if (isSaved)
        {
            throw new ConflictException("Deck is already saved");
        }

        var savedDeck = new SavedDeck
        {
            DeckId = request.DeckId,
            UserId = userId
        };

        await _deckRepository.AddSavedDeckAsync(savedDeck, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
