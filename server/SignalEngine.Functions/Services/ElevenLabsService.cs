using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SignalEngine.Functions.Services;

public class ElevenLabsService(
    HttpClient httpClient,
    IConfiguration config,
    ILogger<ElevenLabsService> logger
)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<Stream> TextToSpeechAsync(string text, CancellationToken ct = default)
    {
        var apiKey =
            config["ELEVEN_LABS_API_KEY"]
            ?? throw new InvalidOperationException("ELEVEN_LABS_API_KEY is not configured.");
        var voiceId =
            config["ELEVEN_LABS_VOICE_KEY"]
            ?? throw new InvalidOperationException("ELEVEN_LABS_VOICE_KEY is not configured.");

        var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}";

        var payload = new
        {
            text,
            model_id = "eleven_flash_v2_5",
            voice_settings = new { stability = 0.5, similarity_boost = 0.75 },
            output_format = "mp3_44100_192",
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("xi-api-key", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        logger.LogInformation("Calling ElevenLabs TTS for voice {VoiceId}", voiceId);

        var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct
        );

        //read the response as a stream and log it if it's an error
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "ElevenLabs TTS request failed with status {StatusCode}: {Error}",
                response.StatusCode,
                errorContent
            );
        }
        response.EnsureSuccessStatusCode();

        // Return the stream — caller is responsible for disposing the response
        return await response.Content.ReadAsStreamAsync(ct);
    }

    public async Task<TtsWithTimestampsResponse> TextToSpeechWithTimestampsAsync(
        string text,
        CancellationToken ct = default
    )
    {
        var apiKey =
            config["ELEVEN_LABS_API_KEY"]
            ?? throw new InvalidOperationException("ELEVEN_LABS_API_KEY is not configured.");
        var voiceId =
            config["ELEVEN_LABS_VOICE_KEY"]
            ?? throw new InvalidOperationException("ELEVEN_LABS_VOICE_KEY is not configured.");

        var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}/with-timestamps";

        var payload = new
        {
            text,
            model_id = "eleven_flash_v2_5",
            voice_settings = new { stability = 0.5, similarity_boost = 0.75 },
            output_format = "mp3_44100_192",
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("xi-api-key", apiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        logger.LogInformation(
            "Calling ElevenLabs TTS with timestamps for voice {VoiceId}",
            voiceId
        );

        var response = await httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "ElevenLabs TTS with timestamps request failed with status {StatusCode}: {Error}",
                response.StatusCode,
                errorContent
            );
        }
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TtsWithTimestampsResponse>(
            JsonOptions,
            ct
        );
        return result ?? throw new InvalidOperationException("Empty response from ElevenLabs.");
    }
}

public record TtsWithTimestampsResponse(
    string AudioBase64,
    TtsAlignment Alignment,
    TtsAlignment NormalizedAlignment
);

public record TtsAlignment(
    string[] Characters,
    double[] CharacterStartTimesSeconds,
    double[] CharacterEndTimesSeconds
);
