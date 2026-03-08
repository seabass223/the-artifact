namespace SignalEngine.Functions.Models;

public record NarrativeClassification
{
    public required string Classification { get; init; }
    public required string Text { get; init; }
    public double? StartTimeSeconds { get; init; }
    public double? EndTimeSeconds { get; init; }
}

// Internal deserialization wrapper for the LLM response
internal record ClassificationResponse(List<NarrativeClassification> Classifications);
