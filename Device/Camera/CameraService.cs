using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using PiPanel.Shared.Camera;

namespace PiPanel.Device.Camera;

public class CameraService
{
    private const string CameraImageTmpFilePath = "tmp/cameraimage.jpg";
    
    private readonly DeviceClient deviceClient;

    public CameraService(DeviceClient deviceClient)
    {
        this.deviceClient = deviceClient;
    }

    public async void ExecuteAsync(object? state)
    {
        await CaptureCameraImagesAsync();
    }

    private async Task CaptureCameraImagesAsync()
    {
        foreach (var camera in CameraList.Cameras)
        {
            try
            {
                await CameraCapture.CaptureImageAsync(camera, CameraImageTmpFilePath);
                await UploadCaptureImage(camera.Label);
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

    private async Task UploadCaptureImage(string label)
    {
        var fileTime = File.GetCreationTime(CameraImageTmpFilePath);
        var blobName = CapturedImageNaming.GetBlobNameForDevice(label, fileTime);

        Console.WriteLine($"Uploading capture image {blobName}");

        var sasUri = await deviceClient.GetFileUploadSasUriAsync(new FileUploadSasUriRequest
        {
            BlobName = blobName,
        });

        var uploadUri = sasUri.GetBlobUri();
        var blobClient = new BlockBlobClient(uploadUri);

        using var fileStream = File.OpenRead(CameraImageTmpFilePath);

        try
        {
            await blobClient.UploadAsync(fileStream, new BlobUploadOptions());
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
    }
}