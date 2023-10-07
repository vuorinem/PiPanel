using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using PiPanel.Server.Options;
using PiPanel.Shared.Camera;

namespace PiPanel.Server.Services;

public class CameraCaptureService
{
    private readonly BlobContainerClient containerClient;
    private readonly StorageOptions options;
    private readonly ILogger<CameraCaptureService> logger;

    public CameraCaptureService(
        IOptions<StorageOptions> optionsSnapshot,
        ILogger<CameraCaptureService> logger)
    {
        this.logger = logger;

        options = optionsSnapshot.Value;
        containerClient = new BlobContainerClient(options.ContainerUri, new DefaultAzureCredential());
    }

    public async Task<IEnumerable<CapturedImage>> GetCameraCaptures(DateOnly date, string label)
    {
        var blobPrefix = CapturedImageNaming.GetBlobPrefixForDate(label, date);
        var blobList = containerClient.GetBlobsAsync(prefix: blobPrefix);

        var images = new List<CapturedImage>();

        await foreach (var blob in blobList)
        {
            var timeCreated = TimeOnly.ParseExact(blob.Name.Substring(blobPrefix.Length, CapturedImageNaming.TimePathFormat.Length), CapturedImageNaming.TimePathFormat);

            images.Add(new CapturedImage
            {
                CreatedAt = new DateTime(date.Year, date.Month, date.Day, timeCreated.Hour, timeCreated.Minute, timeCreated.Second),
                BlobName = blob.Name,
            });
        }

        return images;
    }

    internal async Task<byte[]?> GetCapturedImage(string blobName, CancellationToken cancellationToken)
    {
        var blobClient = containerClient.GetBlobClient(blobName);
        var blobExists = await blobClient.ExistsAsync(cancellationToken);

        if (!blobExists.Value)
        {
            return null;
        }

        var response = await blobClient.DownloadContentAsync(cancellationToken: cancellationToken);

        return response.Value.Content.ToArray();
    }
}