using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SignalEngine.Functions.Services;

namespace SignalEngine.Functions.Functions;

public class SignalScanFunction(
    OrchestrationService orchestration,
    ILogger<SignalScanFunction> logger
)
{
    [Function("SignalScan")]
    public async Task Run(
        [TimerTrigger("0 0 20 * * *", RunOnStartup = false)] TimerInfo timer,
        CancellationToken ct
    )
    {
        logger.LogInformation("SignalScan timer fired at {Time}", DateTime.UtcNow);
        await orchestration.RunFlow1Async(ct: ct);
    }
}
