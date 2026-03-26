using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.User.Commands.RecordDeckView;

public record RecordDeckViewCommand(Guid DeckId) : IRequest;

public class RecordDeckViewCommandHandler : IRequestHandler<RecordDeckViewCommand>
{
    private readonly IRepository<Guid, DeckView> _deckViewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RecordDeckViewCommandHandler(
        IRepository<Guid, DeckView> deckViewRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _deckViewRepository = deckViewRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(RecordDeckViewCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!;

        var existing = await _deckViewRepository.GetQueryable()
            .FirstOrDefaultAsync(v => v.DeckId == request.DeckId && v.UserId == userId, cancellationToken);

        if (existing is not null)
        {
            existing.ViewedAt = DateTime.UtcNow;
            _deckViewRepository.Update(existing);
        }
        else
        {
            await _deckViewRepository.AddAsync(new DeckView
            {
                DeckId = request.DeckId,
                UserId = userId,
                ViewedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
