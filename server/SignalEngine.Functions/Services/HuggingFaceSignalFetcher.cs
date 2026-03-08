using System.Text.Json;
using Microsoft.Extensions.Logging;
using SignalEngine.Functions.Models;

namespace SignalEngine.Functions.Services;

public class HuggingFaceSignalFetcher(HttpClient http, ILogger<HuggingFaceSignalFetcher> logger)
    : ISignalFetcher
{
    public string SourceName => "HuggingFace";

    public async Task<List<RawSignal>> FetchSignalsAsync(CancellationToken ct = default)
    {
        var signals = new List<RawSignal>();

        try
        {
            // Fetch trending text-generation / conversational models from HuggingFace
            var allowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "text-generation",
                "text2text-generation",
                "conversational",
                "question-answering",
            };

            var url =
                "https://huggingface.co/api/models?sort=likes&limit=10&pipeline_tag=text-generation";
            var response = await http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("HuggingFace API returned {Status}", response.StatusCode);
                return signals;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            foreach (var item in doc.RootElement.EnumerateArray().Take(10))
            {
                var modelId = item.GetProperty("modelId").GetString()!;

                signals.Add(
                    new RawSignal
                    {
                        Title = modelId,
                        Description = item.TryGetProperty("pipeline_tag", out var tag)
                            ? tag.GetString()
                            : null,
                        Url = $"https://huggingface.co/{modelId}",
                        Source = SourceName,
                        Timestamp = item.TryGetProperty("createdAt", out var created)
                            ? DateTime.Parse(created.GetString()!)
                            : DateTime.UtcNow,
                        Metrics = new Dictionary<string, object>
                        {
                            ["downloads"] = item.TryGetProperty("downloads", out var dl)
                                ? dl.GetInt64()
                                : 0,
                            ["likes"] = item.TryGetProperty("likes", out var likes)
                                ? likes.GetInt32()
                                : 0,
                            ["pipeline_tag"] = item.TryGetProperty("pipeline_tag", out var pt)
                                ? pt.GetString() ?? "unknown"
                                : "unknown",
                        },
                    }
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch HuggingFace signals");
        }

        logger.LogInformation("Fetched {Count} signals from HuggingFace", signals.Count);
        return signals;
    }
}
