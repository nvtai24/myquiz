using MediatR;
using MyQuizGenerator.Application.Auth.DTOs;
using MyQuizGenerator.Application.Common.Interfaces;

namespace MyQuizGenerator.Application.Auth.Commands.Logout;

public record LogoutCommand(LogoutRequest Request) : IRequest;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Guid, Domain.Entities.RefreshToken> _refreshTokenRepository;

    public LogoutCommandHandler(
        IUnitOfWork unitOfWork,
        IRepository<Guid, Domain.Entities.RefreshToken> refreshTokenRepository)
    {
        _unitOfWork = unitOfWork;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var refreshToken = command.Request.RefreshToken;

        var storedToken = _refreshTokenRepository.GetQueryable()
            .FirstOrDefault(x => x.Token == refreshToken);

        if (storedToken != null)
        {
            storedToken.Invalidated = true;
            _refreshTokenRepository.Update(storedToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
