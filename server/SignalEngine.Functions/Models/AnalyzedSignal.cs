namespace SignalEngine.Functions.Models;

public record AnalyzedSignal
{
    public required RawSignal Raw { get; init; }
    public required string Field { get; init; }
    public required string Interpretation { get; init; }
    public required double Novelty { get; init; }
    public required double Momentum { get; init; }
    public required double Depth { get; init; }
    public double AnomalyIndex => (Novelty * 0.5) + (Momentum * 0.3) + (Depth * 0.2);
    public bool IsDeepSignal => AnomalyIndex >= 8.5;
}
