using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SignalEngine.Functions.Services;

namespace SignalEngine.Functions.Functions;

public class OrchestrationFunction(
    OrchestrationService orchestration,
    ILogger<OrchestrationFunction> logger
)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// GET /api/orchestrate/flow1
    /// Full pipeline: scan → report → narrate → TTS → mp3 → JSON result.
    /// </summary>
    [Function("OrchestrateFlow1")]
    public async Task<HttpResponseData> Flow1(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orchestrate/flow1")]
            HttpRequestData req,
        CancellationToken ct
    )
    {
        logger.LogInformation("[Flow1] HTTP trigger invoked");
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var dateParam = query["date"];
        var scanDateFolder = DateTime.TryParseExact(
            dateParam,
            "yyyy-MM-dd",
            null,
            System.Globalization.DateTimeStyles.None,
            out _
        )
            ? dateParam
            : null;
        try
        {
            var result = await orchestration.RunFlow1Async(scanDateFolder, ct);
            return await JsonResponseAsync(req, result, ct);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteStringAsync(ex.Message, ct);
            return response;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static async Task<HttpResponseData> JsonResponseAsync<T>(
        HttpRequestData req,
        T data,
        CancellationToken ct
    )
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        response.Headers.Add("Content-Type", "application/json");
        await response.Body.WriteAsync(Encoding.UTF8.GetBytes(json), ct);
        return response;
    }
}
