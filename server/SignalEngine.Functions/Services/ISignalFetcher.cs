using SignalEngine.Functions.Models;

namespace SignalEngine.Functions.Services;

public interface ISignalFetcher
{
    string SourceName { get; }
    Task<List<RawSignal>> FetchSignalsAsync(CancellationToken ct = default);
}
