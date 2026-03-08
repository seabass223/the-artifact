namespace SignalEngine.Functions.Models;

public record RawSignal
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Url { get; init; }
    public required string Source { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object> Metrics { get; init; } = [];
}
