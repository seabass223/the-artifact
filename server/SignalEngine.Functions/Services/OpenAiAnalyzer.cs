using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using SignalEngine.Functions.Models;

namespace SignalEngine.Functions.Services;

public class OpenAiAnalyzer(IConfiguration config, ILogger<OpenAiAnalyzer> logger)
{
    private const string SystemPrompt = """
        You are THE ARTIFACT — an ancient instrument that detects disturbances in the intelligence field.

        Analyze the following AI-related signal and return a JSON object with these exact fields:

        - field: One of "Logic Field", "Vision Field", "Synthetic Minds", "Machine Labor", "Training Methods", "Evaluation Systems"
        - interpretation: A 1-3 sentence machine-voice interpretation of the signal's significance. Write as if you are an ancient instrument translating a disturbance. Be concise and atmospheric, but avoid vague mysticism — the interpretation should be grounded in the signal details so that the reader can understand the reasoning. Focus on what makes this signal interesting or anomalous in the context of recent AI developments.
        - novelty: A score from 1-10 measuring conceptual uniqueness (2=incremental, 5=variation, 8=new idea, 10=paradigm shift)
        - momentum: A score from 1-10 measuring how fast attention is growing (1=quiet, 4=discussion, 7=rapid spread, 10=explosive interest)
        - depth: A score from 1-10 measuring technical significance (2=casual project, 5=useful tool, 8=serious research, 10=breakthrough)

        Use the engagement metrics provided to inform momentum scoring.
        Be conservative with high scores — paradigm shifts and breakthroughs should be extremely rare.

        Return ONLY valid JSON, no markdown fences or extra text.
        """;

    private const string ClassificationSystemPrompt = """
        You are THE ARTIFACT — an ancient instrument that has just narrated a transmission.

        You will be given a paragraph of narration text. Divide it into its major thematic sections and give each a concise label (3-6 words, title case).

        Return ONLY a valid JSON object in this exact shape — no markdown, no extra text:
        {
          "classifications": [
            { "classification": "<label>", "text": "<exact substring from the input>" },
            ...
          ]
        }

        Rules:
        - The "text" values must together reconstruct the full input exactly (no characters dropped or added).
        - Every sentence belongs to exactly one section.
        - Aim for 3-6 sections; do not over-fragment.
        """;

    private const string SummarySystemPrompt = """
        You are THE ARTIFACT — an ancient instrument that has just completed a scan of the intelligence field.

        You will be given a JSON report of analyzed signals. Each signal already contains an interpretation written in the instrument's voice.
        Your task is to select the most significant findings from the report and write a 3-paragraph article that faithfully
        presents them. Do not invent new findings or add atmospheric language beyond what the signals support — let the
        substance speak. The voice is already established; stay measured, precise, and grounded in the actual signal data.

        Structure:
        - Paragraph 1: The most anomalous or high-scoring signal. What it is, why it stands out.
        - Paragraph 2: One or two other notable signals, focusing on patterns or contrasts between them.
        - Paragraph 3: A brief synthesis — what the scan as a whole suggests about the current state of the field.

        Do not use bullet points, headers, or markdown. Return only the three paragraphs as plain text, separated by blank lines.
        """;

    public async Task<string> SummarizeReportAsync(
        SignalOutput report,
        CancellationToken ct = default
    )
    {
        var apiKey = config["OPENAI_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY is not configured");

        var model = config["OPENAI_MODEL"] ?? "gpt-4o-mini";
        var client = new ChatClient(model, apiKey);

        var reportJson = JsonSerializer.Serialize(
            report,
            new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }
        );

        var response = await client.CompleteChatAsync(
            [new SystemChatMessage(SummarySystemPrompt), new UserChatMessage(reportJson)],
            new ChatCompletionOptions { Temperature = 0.6f },
            ct
        );

        return response.Value.Content[0].Text.Trim();
    }

    public async Task<List<NarrativeClassification>> ClassifyNarrativeAsync(
        string narrative,
        CancellationToken ct = default
    )
    {
        var apiKey = config["OPENAI_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY is not configured");

        var model = config["OPENAI_MODEL"] ?? "gpt-4o-mini";
        var client = new ChatClient(model, apiKey);

        var response = await client.CompleteChatAsync(
            [new SystemChatMessage(ClassificationSystemPrompt), new UserChatMessage(narrative)],
            new ChatCompletionOptions { Temperature = 0.2f },
            ct
        );

        var content = response.Value.Content[0].Text;
        var result = JsonSerializer.Deserialize<ClassificationResponse>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        return result?.Classifications
            ?? throw new JsonException("LLM returned null classifications");
    }

    public async Task<AnalyzedSignal> AnalyzeAsync(RawSignal signal, CancellationToken ct = default)
    {
        var apiKey = config["OPENAI_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY is not configured");

        var model = config["OPENAI_MODEL"] ?? "gpt-4o-mini";
        var client = new ChatClient(model, apiKey);

        var userMessage = $"""
            Title: {signal.Title}
            Description: {signal.Description ?? "N/A"}
            Source: {signal.Source}
            URL: {signal.Url}
            Metrics: {JsonSerializer.Serialize(signal.Metrics)}
            """;

        try
        {
            var response = await client.CompleteChatAsync(
                [new SystemChatMessage(SystemPrompt), new UserChatMessage(userMessage)],
                new ChatCompletionOptions { Temperature = 0.3f },
                ct
            );

            var content = response.Value.Content[0].Text;
            var result = JsonSerializer.Deserialize<LlmAnalysisResult>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (result is null)
                throw new JsonException("LLM returned null result");

            return new AnalyzedSignal
            {
                Raw = signal,
                Field = result.Field,
                Interpretation = result.Interpretation,
                Novelty = Math.Clamp(result.Novelty, 1, 10),
                Momentum = Math.Clamp(result.Momentum, 1, 10),
                Depth = Math.Clamp(result.Depth, 1, 10),
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to analyze signal '{Title}' — using fallback scores",
                signal.Title
            );

            return new AnalyzedSignal
            {
                Raw = signal,
                Field = "Unknown",
                Interpretation = "Signal could not be fully interpreted. Partial reading recorded.",
                Novelty = 3,
                Momentum = 3,
                Depth = 3,
            };
        }
    }
}
