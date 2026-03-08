namespace SignalEngine.Functions.Models;

public record OrchestrationResult
{
    public required string Flow { get; init; }
    public required DateTime ExecutedUtc { get; init; }
    public required SignalOutput Report { get; init; }
    public string? NarrativeParagraph { get; init; }
    public string? AudioBlobPath { get; init; }
    public string? TimingsBlobPath { get; init; }
    public List<NarrativeClassification>? ArticleClassifications { get; init; }
}
