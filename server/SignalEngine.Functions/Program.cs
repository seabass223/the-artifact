using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SignalEngine.Functions.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register HttpClient for each fetcher
builder.Services.AddHttpClient<GitHubSignalFetcher>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "SignalEngine/1.0");
});
builder.Services.AddHttpClient<HuggingFaceSignalFetcher>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "SignalEngine/1.0");
});
builder.Services.AddHttpClient<ArxivSignalFetcher>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "SignalEngine/1.0");
});
builder.Services.AddHttpClient<XSignalFetcher>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "SignalEngine/1.0");
});

// Register signal fetchers
builder.Services.AddSingleton<ISignalFetcher>(sp => sp.GetRequiredService<GitHubSignalFetcher>());
builder.Services.AddSingleton<ISignalFetcher>(sp =>
    sp.GetRequiredService<HuggingFaceSignalFetcher>()
);
builder.Services.AddSingleton<ISignalFetcher>(sp => sp.GetRequiredService<ArxivSignalFetcher>());
builder.Services.AddSingleton<ISignalFetcher>(sp => sp.GetRequiredService<XSignalFetcher>());

// Register services
builder.Services.AddSingleton<OpenAiAnalyzer>();
builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddSingleton<OrchestrationService>();
builder.Services.AddHttpClient<ElevenLabsService>();

builder.Build().Run();
