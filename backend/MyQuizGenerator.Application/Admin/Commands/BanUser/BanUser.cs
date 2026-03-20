using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;

namespace MyQuizGenerator.Application.Admin.Commands.BanUser;

public record BanUserCommand(string UserId, bool IsBanned) : IRequest;

public class BanUserCommandHandler : IRequestHandler<BanUserCommand>
{
    private readonly IAuthService _authService;

    public BanUserCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task Handle(BanUserCommand request, CancellationToken cancellationToken)
    {
        await _authService.BanUserAsync(request.UserId, request.IsBanned);
    }
}
