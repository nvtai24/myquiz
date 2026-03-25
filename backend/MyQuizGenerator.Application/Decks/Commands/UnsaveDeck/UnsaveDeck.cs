using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Common.Services;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Decks.Commands.UnsaveDeck;

public record UnsaveDeckCommand(Guid DeckId) : IRequest;

public class UnsaveDeckCommandHandler : IRequestHandler<UnsaveDeckCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;

    public UnsaveDeckCommandHandler(
        IUnitOfWork unitOfWork,
        IDeckRepository deckRepository,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UnsaveDeckCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException();

        var savedDeck = await _deckRepository.GetSavedDeckAsync(request.DeckId, userId, cancellationToken)
            ?? throw new NotFoundException("Saved deck not found");

        _deckRepository.RemoveSavedDeck(savedDeck);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
