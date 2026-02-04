using MediatR;

namespace MyQuizGenerator.Application.Ping.Queries.GetPing;

public record GetPingQuery : IRequest<PingResponse>;
