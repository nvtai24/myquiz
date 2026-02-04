using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Features.Auth.DTOs;

namespace MyQuizGenerator.Application.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery(string UserId) : IRequest<UserInfo>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserInfo>
{
    private readonly IAuthService _authService;

    public GetCurrentUserQueryHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<UserInfo> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userInfo = await _authService.GetUserByIdAsync(request.UserId);

        if (userInfo == null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        return userInfo;
    }
}
