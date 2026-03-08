using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SignalEngine.Functions.Models;

namespace SignalEngine.Functions.Services;

public class ArxivSignalFetcher(HttpClient http, ILogger<ArxivSignalFetcher> logger)
    : ISignalFetcher
{
    public string SourceName => "arXiv";

    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";

    public async Task<List<RawSignal>> FetchSignalsAsync(CancellationToken ct = default)
    {
        var signals = new List<RawSignal>();

        try
        {
            // Search arXiv for recent AI agent / coding / LLM papers
            var categories = new[] { "cs.AI", "cs.LG", "cs.CL" };
            var catQuery = string.Join("+OR+", categories.Select(c => $"cat:{c}"));
            var keywords = new[]
            {
                "agent",
                "tool use",
                "code generation",
                "LLM",
                "reasoning",
                "function calling",
                "agentic",
                "RAG",
                "retrieval augmented",
            };
            var kwQuery = string.Join(
                "+OR+",
                keywords.Select(k => $"ti:\"{Uri.EscapeDataString(k)}\"")
            );
            var url =
                $"http://export.arxiv.org/api/query?search_query=({catQuery})+AND+({kwQuery})&sortBy=submittedDate&sortOrder=descending&max_results=15";

            var response = await http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("arXiv API returned {Status}", response.StatusCode);
                return signals;
            }

            var xml = await response.Content.ReadAsStringAsync(ct);
            var doc = XDocument.Parse(xml);

            var entries = doc.Descendants(Atom + "entry");
            foreach (var entry in entries)
            {
                var title =
                    entry.Element(Atom + "title")?.Value.Trim().Replace("\n", " ") ?? "Untitled";
                var summary = entry.Element(Atom + "summary")?.Value.Trim().Replace("\n", " ");
                var link =
                    entry
                        .Elements(Atom + "link")
                        .FirstOrDefault(l => l.Attribute("type")?.Value == "text/html")
                        ?.Attribute("href")
                        ?.Value
                    ?? entry.Element(Atom + "id")?.Value
                    ?? "";
                var published = entry.Element(Atom + "published")?.Value;

                var authorCount = entry.Elements(Atom + "author").Count();

                signals.Add(
                    new RawSignal
                    {
                        Title = title,
                        Description = summary?.Length > 300 ? summary[..300] + "..." : summary,
                        Url = link,
                        Source = SourceName,
                        Timestamp = published != null ? DateTime.Parse(published) : DateTime.UtcNow,
                        Metrics = new Dictionary<string, object>
                        {
                            ["author_count"] = authorCount,
                            ["categories"] = string.Join(
                                ", ",
                                entry
                                    .Elements(Atom + "category")
                                    .Select(c => c.Attribute("term")?.Value ?? "")
                                    .Where(c => !string.IsNullOrEmpty(c))
                            ),
                        },
                    }
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch arXiv signals");
        }

        logger.LogInformation("Fetched {Count} signals from arXiv", signals.Count);
        return signals;
    }
}
