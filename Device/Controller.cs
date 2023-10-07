using System.Text.Json;
using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using PiPanel.Shared.Camera;

public class Controller
{
    private readonly DeviceClient deviceClient;

    private CancellationTokenSource? sleepTokenSource;

    private bool isRunning = false;

    private TimeSpan cameraInterval = TimeSpan.FromSeconds(600);

    private const string CameraImageTmpFilePath = "tmp/cameraimage.jpg";

    public Controller(DeviceClient deviceClient)
    {
        this.deviceClient = deviceClient;
    }

    public async Task RunAsync()
    {
        isRunning = true;

        deviceClient.SetConnectionStatusChangesHandler(OnConnectionStatusChanges);
        await deviceClient.SetReceiveMessageHandlerAsync(OnReceiveMessage, null);
        await deviceClient.SetMethodHandlerAsync(MethodNames.SetCameraInterval, OnSetCameraIntervalAsync, null);

        while (isRunning)
        {
            await CaptureCameraImagesAsync();

            try
            {
                using (sleepTokenSource = new CancellationTokenSource())
                {
                    await Task.Delay(cameraInterval, sleepTokenSource.Token);
                }
            }
            catch (TaskCanceledException)
            {
                continue;
            }
        }
    }

    public void Stop()
    {
        isRunning = false;
        sleepTokenSource?.Cancel();
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
        var fileTime = File.GetCreationTimeUtc(CameraImageTmpFilePath);
        var datePath = fileTime.ToString("yyyy-MM-dd");
        var timePath = fileTime.ToString("HH-mm-ss");
        var blobName = $"{datePath}/{label}/{timePath}.jpg";

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

    private async Task<MethodResponse> OnSetCameraIntervalAsync(MethodRequest methodRequest, object userContext)
    {
        await Task.CompletedTask;

        try
        {
            var intervalInSeconds = JsonSerializer.Deserialize<int>(methodRequest.DataAsJson);

            Console.WriteLine($"Hub set camera interval to {intervalInSeconds} seconds");

            cameraInterval = TimeSpan.FromSeconds(intervalInSeconds);

            sleepTokenSource?.Cancel();

            return new MethodResponse(0);
        }
        catch
        {
            Console.WriteLine($"Hub set camera intercal with invalid value '{methodRequest.Data}'");

            return new MethodResponse((int)MethodResponseStatusCode.BadRequest);
        }
    }

    private async Task OnReceiveMessage(Message message, object userContext)
    {
        using var bodyReader = new StreamReader(message.BodyStream);
        var body = await bodyReader.ReadToEndAsync();

        Console.WriteLine($"Received message: {body}");

        await deviceClient.CompleteAsync(message);
    }

    private void OnConnectionStatusChanges(ConnectionStatus status, ConnectionStatusChangeReason reason)
    {
        Console.WriteLine($"Connection status: {status} ({reason})");
    }
}
