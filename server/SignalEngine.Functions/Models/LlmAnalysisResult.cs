namespace SignalEngine.Functions.Models;

public record LlmAnalysisResult
{
    public required string Field { get; init; }
    public required string Interpretation { get; init; }
    public required double Novelty { get; init; }
    public required double Momentum { get; init; }
    public required double Depth { get; init; }
}
