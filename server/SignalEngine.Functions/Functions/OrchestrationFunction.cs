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
    /// Returns cached artifacts only — never triggers external service calls.
    /// Returns 404 if the cache is incomplete for the requested date.
    /// </summary>
    [Function("OrchestrateFlow1")]
    public async Task<HttpResponseData> Flow1(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orchestrate/flow1")]
            HttpRequestData req,
        CancellationToken ct
    )
    {
        logger.LogInformation("[Flow1] HTTP trigger invoked (cache-only)");
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

        var result = await orchestration.TryGetCachedFlow1Async(scanDateFolder, ct);
        if (result is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync(
                "No cached data available for the requested date. The timer trigger will populate the cache.",
                ct
            );
            return notFound;
        }

        return await JsonResponseAsync(req, result, ct);
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
