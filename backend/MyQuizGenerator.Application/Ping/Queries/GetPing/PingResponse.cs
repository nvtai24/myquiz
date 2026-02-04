namespace MyQuizGenerator.Application.Ping.Queries.GetPing;

public record PingResponse
{
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Version { get; init; } = string.Empty;
}
