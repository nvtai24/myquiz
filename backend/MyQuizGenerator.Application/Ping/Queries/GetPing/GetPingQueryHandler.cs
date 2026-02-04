using MediatR;

namespace MyQuizGenerator.Application.Ping.Queries.GetPing;

public class GetPingQueryHandler : IRequestHandler<GetPingQuery, PingResponse>
{
    public Task<PingResponse> Handle(GetPingQuery request, CancellationToken cancellationToken)
    {
        var response = new PingResponse
        {
            Message = "Pong! MediatR is working!",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        };

        return Task.FromResult(response);
    }
}
