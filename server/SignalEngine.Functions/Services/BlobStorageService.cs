using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SignalEngine.Functions.Services;

public class BlobStorageService(IConfiguration config, ILogger<BlobStorageService> logger)
{
    private static readonly BlobCorsRule AudioCorsRule = new()
    {
        AllowedOrigins = "http://localhost:3000",
        AllowedMethods = "GET,HEAD,OPTIONS",
        AllowedHeaders = "*",
        ExposedHeaders = "Content-Length,Content-Type,Content-Range",
        MaxAgeInSeconds = 3600,
    };

    private async Task EnsureCorsAsync(BlobServiceClient serviceClient, CancellationToken ct)
    {
        try
        {
            var props = (await serviceClient.GetPropertiesAsync(ct)).Value;
            bool alreadySet = props.Cors.Any(r => r.AllowedOrigins == AudioCorsRule.AllowedOrigins);
            if (!alreadySet)
            {
                props.Cors.Clear();
                props.Cors.Add(AudioCorsRule);
                await serviceClient.SetPropertiesAsync(props, ct);
                logger.LogInformation("Blob service CORS configured");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Could not set blob service CORS — audio waveform may not work cross-origin"
            );
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task UploadAsync<T>(T data, string blobName, CancellationToken ct = default)
    {
        var connectionString =
            config["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException("AzureWebJobsStorage is not configured");

        var containerName = config["SIGNAL_CONTAINER"] ?? "signals";
        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);
        await EnsureCorsAsync(blobServiceClient, ct);

        // Set public access for the frontend to read directly
        await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob, cancellationToken: ct);

        var blobClient = containerClient.GetBlobClient(blobName);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        using var stream = new MemoryStream(bytes);
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: ct);

        logger.LogInformation(
            "Uploaded signal data to blob '{BlobName}' ({Bytes} bytes)",
            blobName,
            bytes.Length
        );
    }

    /// <summary>Downloads and deserializes a JSON blob. Returns null if the blob does not exist.</summary>
    public async Task<T?> TryDownloadJsonAsync<T>(string blobName, CancellationToken ct = default)
    {
        var connectionString =
            config["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException("AzureWebJobsStorage is not configured");

        var containerName = config["SIGNAL_CONTAINER"] ?? "signals";
        var blobClient = new BlobServiceClient(connectionString)
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(ct))
            return default;

        var download = await blobClient.DownloadContentAsync(ct);
        logger.LogInformation("Cache hit — loaded '{BlobName}' from blob storage", blobName);
        return JsonSerializer.Deserialize<T>(download.Value.Content, JsonOptions);
    }

    /// <summary>Returns the URI for a blob if it exists, otherwise null.</summary>
    public async Task<string?> TryGetBlobUriAsync(string blobName, CancellationToken ct = default)
    {
        var connectionString =
            config["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException("AzureWebJobsStorage is not configured");

        var containerName = config["SIGNAL_CONTAINER"] ?? "signals";
        var blobClient = new BlobServiceClient(connectionString)
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(ct))
            return null;

        logger.LogInformation("Cache hit — '{BlobName}' already exists in blob storage", blobName);
        return blobClient.Uri.ToString();
    }

    /// <summary>Uploads a raw stream and returns the blob URI.</summary>
    public async Task<string> UploadStreamAsync(
        Stream stream,
        string blobName,
        string contentType,
        CancellationToken ct = default
    )
    {
        var connectionString =
            config["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException("AzureWebJobsStorage is not configured");

        var containerName = config["SIGNAL_CONTAINER"] ?? "signals";
        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);
        await EnsureCorsAsync(blobServiceClient, ct);
        await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob, cancellationToken: ct);

        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(
            stream,
            new Azure.Storage.Blobs.Models.BlobUploadOptions
            {
                HttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders
                {
                    ContentType = contentType,
                },
            },
            ct
        );

        logger.LogInformation("Uploaded stream to blob '{BlobName}'", blobName);

        return blobClient.Uri.ToString();
    }
}
