using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Constants;

namespace MyQuizGenerator.Application.Admin.Commands.AssignRole;

public record AssignRoleCommand(string UserId, string Role) : IRequest;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand>
{
    private readonly IAuthService _authService;

    public AssignRoleCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        if (!Roles.All.Contains(request.Role))
        {
            throw new BadRequestException($"Invalid role '{request.Role}'. Valid roles: {string.Join(", ", Roles.All)}");
        }

        await _authService.AssignRoleAsync(request.UserId, request.Role);
    }
}
