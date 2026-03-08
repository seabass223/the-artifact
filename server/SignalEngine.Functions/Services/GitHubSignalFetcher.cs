using System.Text.Json;
using Microsoft.Extensions.Logging;
using SignalEngine.Functions.Models;

namespace SignalEngine.Functions.Services;

public class GitHubSignalFetcher(HttpClient http, ILogger<GitHubSignalFetcher> logger)
    : ISignalFetcher
{
    public string SourceName => "GitHub";

    public async Task<List<RawSignal>> FetchSignalsAsync(CancellationToken ct = default)
    {
        var signals = new List<RawSignal>();

        try
        {
            // Search GitHub for recently created AI agent / LLM tooling repos
            var queries = new[]
            {
                "AI+agent",
                "LLM+agent+framework",
                "tool+use+LLM",
                "MCP+server",
                "openai+agents",
                "claude+API",
                "agentic+coding",
                "function+calling+LLM",
            };

            var since = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");

            foreach (var query in queries)
            {
                var url =
                    $"https://api.github.com/search/repositories?q={query}+created:>{since}&sort=stars&order=desc&per_page=10";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Accept", "application/vnd.github.v3+json");

                var response = await http.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "GitHub API returned {Status} for query '{Query}'",
                        response.StatusCode,
                        query
                    );
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                var items = doc.RootElement.GetProperty("items");

                foreach (var item in items.EnumerateArray())
                {
                    var fullName = item.GetProperty("full_name").GetString()!;
                    // Deduplicate by URL
                    var repoUrl = item.GetProperty("html_url").GetString()!;
                    if (signals.Any(s => s.Url == repoUrl))
                        continue;

                    signals.Add(
                        new RawSignal
                        {
                            Title = fullName,
                            Description = item.GetProperty("description").GetString(),
                            Url = repoUrl,
                            Source = SourceName,
                            Timestamp = item.TryGetProperty("created_at", out var created)
                                ? DateTime.Parse(created.GetString()!)
                                : DateTime.UtcNow,
                            Metrics = new Dictionary<string, object>
                            {
                                ["stars"] = item.GetProperty("stargazers_count").GetInt32(),
                                ["forks"] = item.GetProperty("forks_count").GetInt32(),
                                ["watchers"] = item.GetProperty("watchers_count").GetInt32(),
                                ["language"] =
                                    item.GetProperty("language").GetString() ?? "unknown",
                            },
                        }
                    );
                }

                // Small delay to respect rate limits
                await Task.Delay(500, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch GitHub signals");
        }

        logger.LogInformation("Fetched {Count} signals from GitHub", signals.Count);
        return signals;
    }
}
