using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SignalEngine.Functions.Services;

namespace SignalEngine.Functions.Functions;

public class TextToSpeechFunction(
    ElevenLabsService elevenLabs,
    ILogger<TextToSpeechFunction> logger
)
{
    [Function("TextToSpeech")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "tts")] HttpRequestData req,
        CancellationToken ct
    )
    {
        string? text;
        try
        {
            var body = await req.ReadAsStringAsync();
            text = body?.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(text))
            {
                var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Request body must contain the text to synthesize.", ct);
                return bad;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read request body");
            var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Could not read request body.", ct);
            return bad;
        }

        Stream audioStream;
        try
        {
            audioStream = await elevenLabs.TextToSpeechAsync(text, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "ElevenLabs API request failed");
            var error = req.CreateResponse(System.Net.HttpStatusCode.BadGateway);
            await error.WriteStringAsync("ElevenLabs API request failed.", ct);
            return error;
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "audio/mpeg");
        await audioStream.CopyToAsync(response.Body, ct);

        return response;
    }
}
