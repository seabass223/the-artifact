using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SignalEngine.Functions.Models;

namespace SignalEngine.Functions.Services;

public class XSignalFetcher(HttpClient http, IConfiguration config, ILogger<XSignalFetcher> logger)
    : ISignalFetcher
{
    public string SourceName => "X";

    public async Task<List<RawSignal>> FetchSignalsAsync(CancellationToken ct = default)
    {
        var signals = new List<RawSignal>();
        var bearerToken = config["X_BEARER_TOKEN"];

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            logger.LogWarning("X_BEARER_TOKEN not configured — skipping X.com signal fetch");
            return signals;
        }

        try
        {
            // Search recent tweets about AI agents, tools, and developer-focused LLM content
            var query =
                "(\"AI agent\" OR \"LLM agent\" OR \"agentic\" OR \"function calling\" OR \"tool use\" OR \"MCP\" OR \"Claude API\" OR \"OpenAI agents\" OR \"coding agent\" OR \"agent framework\") -is:retweet lang:en";
            var encodedQuery = Uri.EscapeDataString(query);
            var url =
                $"https://api.x.com/2/tweets/search/recent?query={encodedQuery}&max_results=10&tweet.fields=created_at,public_metrics,author_id&expansions=author_id&user.fields=username,public_metrics";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("X API returned {Status}", response.StatusCode);
                return signals;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out var data))
            {
                logger.LogWarning("X API returned no data");
                return signals;
            }

            // Build author lookup
            var authors = new Dictionary<string, string>();
            if (
                doc.RootElement.TryGetProperty("includes", out var includes)
                && includes.TryGetProperty("users", out var users)
            )
            {
                foreach (var user in users.EnumerateArray())
                {
                    var id = user.GetProperty("id").GetString()!;
                    var username = user.GetProperty("username").GetString()!;
                    authors[id] = username;
                }
            }

            foreach (var tweet in data.EnumerateArray())
            {
                var tweetId = tweet.GetProperty("id").GetString()!;
                var text = tweet.GetProperty("text").GetString()!;
                var authorId = tweet.GetProperty("author_id").GetString()!;
                var username = authors.GetValueOrDefault(authorId, "unknown");

                var metrics = tweet.TryGetProperty("public_metrics", out var pm) ? pm : default;

                signals.Add(
                    new RawSignal
                    {
                        Title = $"@{username}: {(text.Length > 100 ? text[..100] + "..." : text)}",
                        Description = text,
                        Url = $"https://x.com/{username}/status/{tweetId}",
                        Source = SourceName,
                        Timestamp = tweet.TryGetProperty("created_at", out var created)
                            ? DateTime.Parse(created.GetString()!)
                            : DateTime.UtcNow,
                        Metrics = new Dictionary<string, object>
                        {
                            ["likes"] =
                                metrics.ValueKind != JsonValueKind.Undefined
                                    ? metrics.GetProperty("like_count").GetInt32()
                                    : 0,
                            ["retweets"] =
                                metrics.ValueKind != JsonValueKind.Undefined
                                    ? metrics.GetProperty("retweet_count").GetInt32()
                                    : 0,
                            ["replies"] =
                                metrics.ValueKind != JsonValueKind.Undefined
                                    ? metrics.GetProperty("reply_count").GetInt32()
                                    : 0,
                            ["impressions"] =
                                metrics.ValueKind != JsonValueKind.Undefined
                                && metrics.TryGetProperty("impression_count", out var imp)
                                    ? imp.GetInt32()
                                    : 0,
                        },
                    }
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch X signals");
        }

        logger.LogInformation("Fetched {Count} signals from X", signals.Count);
        return signals;
    }
}
