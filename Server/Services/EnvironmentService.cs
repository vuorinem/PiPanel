using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using PiPanel.Server.Models;
using PiPanel.Server.Options;
using PiPanel.Shared.Camera;
using PiPanel.Shared.Environment;
using System.Text.Json;
using System.Threading;

namespace PiPanel.Server.Services;

public class EnvironmentService
{
    private readonly BlobContainerClient containerClient;
    private readonly StorageOptions options;
    private readonly ILogger<EnvironmentService> logger;

    public EnvironmentService(
        IOptions<StorageOptions> optionsSnapshot,
        ILogger<EnvironmentService> logger)
    {
        this.logger = logger;

        options = optionsSnapshot.Value;
        containerClient = new BlobContainerClient(options.EnvironmentContainerUri, new DefaultAzureCredential());
    }

    public async Task<EnvironmentStatus?> GetLatestForDayAsync(DateOnly date)
    {
        var blobNames = await GetBlobNamesForDayAsync(date);

        if (!blobNames.Any())
        {
            return null;
        }

        var environmentStatuses = await GetStatusesFromBlobAsync(blobNames.Last());

        return environmentStatuses.LastOrDefault();
    }

    private async Task<List<string>> GetBlobNamesForDayAsync(DateOnly date)
    {
        var blobPrefix = EnvironmentStatusNaming.GetBlobPrefixForDate(date);
        var blobList = containerClient.GetBlobsAsync(prefix: blobPrefix);

        var blobNames = new List<string>();

        await foreach (var blob in blobList)
        {
            blobNames.Add(blob.Name);
        }

        return blobNames;
    }

    private async Task<List<EnvironmentStatus>> GetStatusesFromBlobAsync(string blobName)
    {
        var blobClient = containerClient.GetBlobClient(blobName);
        var blobExists = await blobClient.ExistsAsync();

        var environmentStatuses = new List<EnvironmentStatus>();

        if (!blobExists.Value)
        {
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

        return environmentStatuses
            .OrderBy(status => status.MeasuredAt)
            .ToList();
    }
}