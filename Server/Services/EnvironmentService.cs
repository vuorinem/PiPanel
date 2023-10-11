using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using PiPanel.Server.Models;
using PiPanel.Server.Options;
using PiPanel.Shared.Environment;
using System.Text.Json;

namespace PiPanel.Server.Services;

public class EnvironmentService
{
    private readonly BlobContainerClient containerClient;
    private readonly StorageOptions storageOptions;
    private readonly IotOptions iotOptions;
    private readonly ILogger<EnvironmentService> logger;

    public EnvironmentService(
        IOptions<StorageOptions> storageOptionsSnapshot,
        IOptions<IotOptions> iotOptionsSnapshot,
        ILogger<EnvironmentService> logger)
    {
        this.logger = logger;

        storageOptions = storageOptionsSnapshot.Value;
        iotOptions = iotOptionsSnapshot.Value;

        containerClient = new BlobContainerClient(storageOptions.EnvironmentContainerUri, new DefaultAzureCredential());
    }

    public async Task<IList<EnvironmentStatus>> GetStatusesByDateAsync(DateOnly date)
    {
        logger.LogDebug("Retrieving environment statuses for {Date}", date);

        (var environmentStatuses, var aggregatedAt) = await GetAggregatedStatusesByDateAsync(date);

        if (aggregatedAt is not null && DateOnly.FromDateTime(aggregatedAt.Value.Date) > date)
        {
            logger.LogDebug("All environment statuses from {Date} are included in the aggregate, no need to get individual blobs", date);
            return environmentStatuses;
        }

        var blobs = await GetBlobNamesForDayAsync(date);

        foreach (var blob in blobs)
        {
            if (aggregatedAt is null || blob.modifiedAt > aggregatedAt)
            {
                environmentStatuses.AddRange(await GetStatusesFromBlobAsync(blob.name));
            }
            else
            {
                logger.LogDebug("Blob {BlobName} already included in the aggregate for {Date}", blob.name, date);
            }
        }

        if (environmentStatuses.Any())
        {
            await SaveAggregatedStatusesByDate(date, environmentStatuses);
        }

        logger.LogDebug("Collected {Count} environment statuses for {Date}", environmentStatuses.Count, date);

        return environmentStatuses;
    }

    private async Task<(List<EnvironmentStatus> environmentStatuses, DateTimeOffset? lastModifiedAt)> GetAggregatedStatusesByDateAsync(DateOnly date)
    {
        logger.LogDebug("Retrieving aggregated environment statuses for {Date}", date);

        var blobName = EnvironmentStatusNaming.GetAggregateBlobNameForDate(iotOptions.HubName, date);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
        {
            logger.LogDebug("No aggregated file found for {Date}", date);

            return (new List<EnvironmentStatus>(), null);
        }

        var blobContent = await blobClient.DownloadContentAsync();

        var environmentStatuses = blobContent.Value.Content.ToObjectFromJson<List<EnvironmentStatus>>();

        logger.LogDebug("Found {Count} aggregated statuses for {Date}", environmentStatuses.Count, date);

        return (environmentStatuses, blobContent.Value.Details.LastModified);
    }

    private async Task SaveAggregatedStatusesByDate(DateOnly date, List<EnvironmentStatus> environmentStatuses)
    {
        logger.LogDebug("Storing {Count} aggregated environment statuses for {Date}", environmentStatuses.Count, date);

        var blobName = EnvironmentStatusNaming.GetAggregateBlobNameForDate(iotOptions.HubName, date);
        var blobClient = containerClient.GetBlobClient(blobName);

        var jsonContent = JsonSerializer.Serialize(environmentStatuses);

        await blobClient.UploadAsync(BinaryData.FromString(jsonContent), new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8",
            },
        });
    }

    private async Task<List<(string name, DateTimeOffset modifiedAt)>> GetBlobNamesForDayAsync(DateOnly date)
    {
        logger.LogDebug("Retrieving environment status blobs for {Date}", date);

        var blobPrefix = EnvironmentStatusNaming.GetBlobPrefixForDate(iotOptions.HubName, date);
        var blobList = containerClient.GetBlobsAsync(prefix: blobPrefix);

        var blobs = new List<(string, DateTimeOffset)>();

        await foreach (var blob in blobList)
        {
            blobs.Add((blob.Name, GetBlobModifiedAt(blob, date, blobPrefix)));
        }

        logger.LogDebug("Found {Count} environment status blobs for {Date}", blobs.Count, date);

        return blobs;
    }

    private async Task<List<EnvironmentStatus>> GetStatusesFromBlobAsync(string blobName)
    {
        logger.LogDebug("Reading environment statuses from blob {BlobName}", blobName);

        var blobClient = containerClient.GetBlobClient(blobName);
        var blobExists = await blobClient.ExistsAsync();

        var environmentStatuses = new List<EnvironmentStatus>();

        if (!blobExists.Value)
        {
            logger.LogWarning("Blob with name {BlobName} does not exist", blobName);

            return environmentStatuses;
        }

        var blobContent = await blobClient.DownloadContentAsync();
        var blobText = blobContent.Value.Content.ToString();
        var lines = blobText.Split("\n");

        for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
        {
            var iotEvent = JsonSerializer.Deserialize<IotHubEvent<EnvironmentStatus>>(lines[lineNumber]);

            if (iotEvent is null)
            {
                logger.LogWarning("Deserializing IoT Event from {BlobName} line {LineNumber} failed", blobName, lineNumber);
                continue;
            }

            environmentStatuses.Add(iotEvent.Body);
        }

        logger.LogDebug("Parsed {Count} environment statuses from blob {BlobName}", environmentStatuses.Count, blobName);

        return environmentStatuses
            .OrderBy(status => status.MeasuredAt)
            .ToList();
    }

    private static DateTimeOffset GetBlobModifiedAt(BlobItem blob, DateOnly date, string blobPrefix)
    {
        if (blob.Properties.LastModified.HasValue)
        {
            return blob.Properties.LastModified.Value;
        }

        var timeFromBlobName = TimeOnly.ParseExact(blob.Name.Substring(blobPrefix.Length, EnvironmentStatusNaming.TimePathFormat.Length), EnvironmentStatusNaming.TimePathFormat);

        return new DateTimeOffset(date.ToDateTime(timeFromBlobName));
    }
}