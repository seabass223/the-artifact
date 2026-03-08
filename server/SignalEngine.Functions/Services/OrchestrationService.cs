using Microsoft.Extensions.Logging;
using SignalEngine.Functions.Models;
using SignalEngine.Functions.Utils;

namespace SignalEngine.Functions.Services;

public class OrchestrationService(
    IEnumerable<ISignalFetcher> fetchers,
    OpenAiAnalyzer analyzer,
    BlobStorageService blobStorage,
    ElevenLabsService elevenLabs,
    ILogger<OrchestrationService> logger
)
{
    /// <summary>
    /// Flow 1: Scan → save report → narrate anomalies → TTS → save mp3 → return result.
    /// </summary>
    public async Task<OrchestrationResult> RunFlow1Async(
        string? scanDateFolder = null,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("[Flow1] Starting full scan + narration + audio pipeline");
        // Timer fires at 20:00 UTC on day N; next run is 20:00 UTC on day N+1.
        DateTime nextRunUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(20);

        // Scan folder represents the upcoming UTC day (timer runs 4 h before midnight).
        var scanFolder = scanDateFolder ?? $"{DateTime.UtcNow.Date.AddDays(1):yyyy-MM-dd}";

        var parsedScanDate = DateTime.Parse(scanFolder);
        // Allow today or tomorrow (timer runs at 20:00 UTC, targeting the next UTC day).
        if (parsedScanDate > DateTime.UtcNow.Date.AddDays(1))
            throw new ArgumentOutOfRangeException(
                nameof(scanDateFolder),
                $"Scan date '{scanFolder}' cannot be more than one day in the future."
            );
        if (parsedScanDate < new DateTime(2026, 3, 7))
            throw new ArgumentOutOfRangeException(
                nameof(scanDateFolder),
                $"Scan date '{scanFolder}' cannot be before 2026-03-07."
            );

        // 1. Report — hourly cache
        var reportBlobName = $"{scanFolder}/report.json";
        var report =
            await blobStorage.TryDownloadJsonAsync<SignalOutput>(reportBlobName, ct)
            ?? await ScanAndSaveReportAsync(reportBlobName, nextRunUtc, ct);

        // 2. Narrative — daily cache
        var narrativeBlobName = $"{scanFolder}/narrative.json";
        var narrative =
            (await blobStorage.TryDownloadJsonAsync<NarrativeCache>(narrativeBlobName, ct))?.Text
            ?? await GenerateAndSaveNarrativeAsync(report, narrativeBlobName, ct);
        logger.LogInformation("[Flow1] Narrative ready ({Chars} chars)", narrative.Length);

        // 3. Audio + timings + classifications — hourly cache (independent of report cache)
        var audioBlobName = $"{scanFolder}/audio.mp3";
        var timingsBlobName = $"{scanFolder}/audio.json";
        var classificationsBlobName = $"{scanFolder}/classifications.json";

        string audioUri;
        string timingsUri;
        List<NarrativeClassification> classifications;

        var cachedAudioUri = await blobStorage.TryGetBlobUriAsync(audioBlobName, ct);
        if (cachedAudioUri is not null)
        {
            audioUri = cachedAudioUri;
            timingsUri = (await blobStorage.TryGetBlobUriAsync(timingsBlobName, ct))!;
            var cached = await blobStorage.TryDownloadJsonAsync<ClassificationResponse>(
                classificationsBlobName,
                ct
            );
            classifications = cached?.Classifications ?? [];
        }
        else
        {
            (audioUri, timingsUri, classifications) = await GenerateAndSaveAudioAsync(
                narrative,
                audioBlobName,
                timingsBlobName,
                classificationsBlobName,
                ct
            );
        }

        return new OrchestrationResult
        {
            Flow = "flow1",
            ExecutedUtc = DateTime.UtcNow,
            Report = report,
            NarrativeParagraph = narrative,
            AudioBlobPath = audioUri,
            TimingsBlobPath = timingsUri,
            ArticleClassifications = classifications,
        };
    }

    /// <summary>
    /// Cache-only variant of Flow 1 — reads blobs only, never calls external services.
    /// Returns null if any required artifact is missing.
    /// </summary>
    public async Task<OrchestrationResult?> TryGetCachedFlow1Async(
        string? scanDateFolder = null,
        CancellationToken ct = default
    )
    {
        var scanFolder = scanDateFolder ?? $"{DateTime.UtcNow:yyyy-MM-dd}";

        var reportBlobName = $"{scanFolder}/report.json";
        var report = await blobStorage.TryDownloadJsonAsync<SignalOutput>(reportBlobName, ct);
        if (report is null)
            return null;

        var narrativeBlobName = $"{scanFolder}/narrative.json";
        var narrative = (await blobStorage.TryDownloadJsonAsync<NarrativeCache>(narrativeBlobName, ct))?.Text;
        if (narrative is null)
            return null;

        var audioBlobName = $"{scanFolder}/audio.mp3";
        var audioUri = await blobStorage.TryGetBlobUriAsync(audioBlobName, ct);
        if (audioUri is null)
            return null;

        var timingsBlobName = $"{scanFolder}/audio.json";
        var timingsUri = await blobStorage.TryGetBlobUriAsync(timingsBlobName, ct);
        if (timingsUri is null)
            return null;

        var classificationsBlobName = $"{scanFolder}/classifications.json";
        var cached = await blobStorage.TryDownloadJsonAsync<ClassificationResponse>(classificationsBlobName, ct);
        var classifications = cached?.Classifications ?? [];

        return new OrchestrationResult
        {
            Flow = "flow1",
            ExecutedUtc = DateTime.UtcNow,
            Report = report,
            NarrativeParagraph = narrative,
            AudioBlobPath = audioUri,
            TimingsBlobPath = timingsUri,
            ArticleClassifications = classifications,
        };
    }

    // -------------------------------------------------------------------------
    // Cache-miss helpers
    // -------------------------------------------------------------------------

    private async Task<string> GenerateAndSaveNarrativeAsync(
        SignalOutput report,
        string blobName,
        CancellationToken ct
    )
    {
        var narrative = await analyzer.SummarizeReportAsync(report, ct);
        await blobStorage.UploadAsync(new NarrativeCache(narrative), blobName, ct);
        logger.LogInformation("[Flow1] Narrative saved to '{Blob}'", blobName);
        return narrative;
    }

    private async Task<SignalOutput> ScanAndSaveReportAsync(
        string blobName,
        DateTime? nextRunUtc,
        CancellationToken ct
    )
    {
        var report = await ScanSignalsAsync(nextRunUtc, ct);
        await blobStorage.UploadAsync(report, blobName, ct);
        logger.LogInformation("[Flow1] Report saved to '{Blob}'", blobName);
        return report;
    }

    private async Task<(
        string audioUri,
        string timingsUri,
        List<NarrativeClassification> classifications
    )> GenerateAndSaveAudioAsync(
        string narrative,
        string audioBlobName,
        string timingsBlobName,
        string classificationsBlobName,
        CancellationToken ct
    )
    {
        var ttsResponse = await elevenLabs.TextToSpeechWithTimestampsAsync(narrative, ct);

        var audioUri = await blobStorage.UploadStreamAsync(
            new MemoryStream(Convert.FromBase64String(ttsResponse.AudioBase64)),
            audioBlobName,
            "audio/mpeg",
            ct
        );
        logger.LogInformation("[Flow1] Audio saved to '{Blob}'", audioBlobName);

        var wordTimings = AlignmentUtils.ToWordTimings(
            ttsResponse.Alignment.Characters,
            ttsResponse.Alignment.CharacterStartTimesSeconds,
            ttsResponse.Alignment.CharacterEndTimesSeconds
        );

        await blobStorage.UploadAsync(wordTimings, timingsBlobName, ct);
        logger.LogInformation("[Flow1] Timings saved to '{Blob}'", timingsBlobName);

        var timingsUri = (await blobStorage.TryGetBlobUriAsync(timingsBlobName, ct))!;

        var sections = await analyzer.ClassifyNarrativeAsync(narrative, ct);
        var timedSections = AlignmentUtils.AttachTimings(sections, wordTimings);

        await blobStorage.UploadAsync(
            new { classifications = timedSections },
            classificationsBlobName,
            ct
        );
        logger.LogInformation("[Flow1] Classifications saved to '{Blob}'", classificationsBlobName);

        return (audioUri, timingsUri, timedSections);
    }

    // -------------------------------------------------------------------------
    // Shared helpers
    // -------------------------------------------------------------------------

    private async Task<SignalOutput> ScanSignalsAsync(DateTime? nextRunUtc, CancellationToken ct)
    {
        var fetchTasks = fetchers.Select(f => f.FetchSignalsAsync(ct));
        var results = await Task.WhenAll(fetchTasks);
        var rawSignals = results.SelectMany(r => r).ToList();

        logger.LogInformation(
            "Fetched {Count} raw signals from {Sources} sources",
            rawSignals.Count,
            fetchers.Count()
        );

        var analyzedSignals = new List<AnalyzedSignal>();
        foreach (var signal in rawSignals)
        {
            var analyzed = await analyzer.AnalyzeAsync(signal, ct);
            analyzedSignals.Add(analyzed);
            await Task.Delay(200, ct);
        }

        return new SignalOutput
        {
            NextRunUtc = nextRunUtc,
            Signals = analyzedSignals
                .Select(a => new SignalEntry
                {
                    Title = a.Raw.Title,
                    Description = a.Raw.Description,
                    Url = a.Raw.Url,
                    Source = a.Raw.Source,
                    Field = a.Field,
                    Interpretation = a.Interpretation,
                    Novelty = a.Novelty,
                    Momentum = a.Momentum,
                    Depth = a.Depth,
                    AnomalyIndex = a.AnomalyIndex,
                    IsDeepSignal = a.IsDeepSignal,
                    Timestamp = a.Raw.Timestamp,
                })
                .OrderByDescending(s => s.AnomalyIndex)
                .ToList(),
        };
    }
}
