using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using PiPanel.Shared;
using PiPanel.Shared.Camera;

namespace PiPanel.Device.Camera;

public class CameraService
{
    private const string CameraImageTmpFilePath = "tmp/cameraimage.jpg";

    private readonly DeviceClient deviceClient;
    private readonly DeviceProperties deviceProperties;

    public CameraService(DeviceClient deviceClient, DeviceProperties deviceProperties)
    {
        this.deviceClient = deviceClient;
        this.deviceProperties = deviceProperties;
    }

    public async Task ExecuteAsync(object? state)
    {
        await CaptureCameraImagesAsync();
    }

    private async Task CaptureCameraImagesAsync()
    {
        foreach (var camera in deviceProperties.Cameras.Where(c => c.Value.IsEnabled))
        {
            try
            {
                await CameraCapture.CaptureImageAsync(camera.Value, CameraImageTmpFilePath);

                if (File.Exists(CameraImageTmpFilePath))
                {
                    await UploadCaptureImage(camera.Key);
                    File.Delete(CameraImageTmpFilePath);
                }
                else
                {
                    Console.Error.WriteLine($"Camera imgage not captured for camera '{camera.Value.Label}'");
                }
            }
            catch (CameraException ex)
            {
                Console.WriteLine($"Exception during image capture: {ex.Message}");
                continue;
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Exception during image upload: {ex.Message}");
                continue;
            }
        }
    }

    private async Task UploadCaptureImage(string cameraKey)
    {
        var fileTime = File.GetCreationTime(CameraImageTmpFilePath);
        var blobName = CapturedImageNaming.GetBlobNameForDevice(cameraKey, fileTime);

        Console.WriteLine("Uploading capture image: {0}", blobName);

        var sasUri = await deviceClient.GetFileUploadSasUriAsync(new FileUploadSasUriRequest
        {
            BlobName = blobName,
        });

        var uploadUri = sasUri.GetBlobUri();
        var blobClient = new BlockBlobClient(uploadUri);

        using var fileStream = File.OpenRead(CameraImageTmpFilePath);

        try
        {
            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "image/jpeg",
                },
            });
        }
        catch
        {
            await deviceClient.CompleteFileUploadAsync(new FileUploadCompletionNotification
            {
                CorrelationId = sasUri.CorrelationId,
                IsSuccess = false,
            });

            return;
        }

        await deviceClient.CompleteFileUploadAsync(new FileUploadCompletionNotification
        {
            CorrelationId = sasUri.CorrelationId,
            IsSuccess = true,
        });

        Console.WriteLine("Capture image successfully uploaded: {0}", blobName);
    }
}