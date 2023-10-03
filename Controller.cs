using System.Text.Json;
using Microsoft.Azure.Devices.Client;

public class Controller
{
    private readonly DeviceClient deviceClient;

    private CancellationToken shutdownToken;

    private CancellationTokenSource? sleepTokenSource;

    private TimeSpan cameraInterval = TimeSpan.FromSeconds(600);

    private const string CameraImageTmpFilePath = "tmp/cameraimage.jpg";

    public Controller(CancellationToken shutdownToken, DeviceClient deviceClient)
    {
        this.shutdownToken = shutdownToken;
        this.deviceClient = deviceClient;
    }

    public async Task RunAsync()
    {
        deviceClient.SetConnectionStatusChangesHandler(OnConnectionStatusChanges);
        await deviceClient.SetReceiveMessageHandlerAsync(OnReceiveMessage, null);
        await deviceClient.SetMethodHandlerAsync(MethodNames.SetCameraInterval, OnSetCameraIntervalAsync, null);

        while (!shutdownToken.IsCancellationRequested)
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

    private async Task CaptureCameraImagesAsync()
    {
        foreach (var camera in CameraCapture.Cameras)
        {
            try
            {
                await CameraCapture.CaptureImageAsync(camera, CameraImageTmpFilePath);
            }
            catch (CameraException ex)
            {
                Console.WriteLine($"Exception during image capture: {ex.Message}");
                continue;
            }

            var timestamp = DateTime.Now.ToShortTimeString();
            File.Copy(CameraImageTmpFilePath, $"tmp/Captures/{camera.Label}_{timestamp}.jpg", true);
        }
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