namespace SignalEngine.Functions.Models;

public record SignalOutput
{
    public DateTime ScanTimestamp { get; init; } = DateTime.UtcNow;
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
    public DateTime? NextRunUtc { get; init; }
    public required List<SignalEntry> Signals { get; init; }
    public int TotalSignals => Signals.Count;
    public int DeepSignalCount => Signals.Count(s => s.IsDeepSignal);
}

public record NarrativeCache(string Text);

public record SignalEntry
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Url { get; init; }
    public required string Source { get; init; }
    public required string Field { get; init; }
    public required string Interpretation { get; init; }
    public required double Novelty { get; init; }
    public required double Momentum { get; init; }
    public required double Depth { get; init; }
    public required double AnomalyIndex { get; init; }
    public required bool IsDeepSignal { get; init; }
    public DateTime Timestamp { get; init; }
}
